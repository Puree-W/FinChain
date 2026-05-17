using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FinChain.Model;
using FinChain.Model.PostgreSQL;
using FinChain.Repository;

namespace FinChain.Function
{
    public interface IChatProcessor
    {
        Task<string> EnsureTopicAsync(ThaiLLMRequestModel req);
        IAsyncEnumerable<string> StreamChatMessageAsync(ThaiLLMRequestModel req, string model, string topicId, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        Task<HistoryLLMModel> GetHistoryMessage(string topicId);
        Task<TopicMessage[]> GetAllHistoryMessage();
        Task<string> TopicLogMessageSave(string message);
        Task LogMessageSave(string content, string role ,string topicId, int order, int promptToken = 0, int completionToken = 0, int totalToken = 0);
        Task<bool> UpdateTopicName(string topicId, string name);
        Task<bool> DeleteTopic(string topicId);
    }
    public class ChatProcessor : IChatProcessor
    {
        private readonly string _apiKey;
        private readonly ILogMessageRepository _logMessageRepository;
        private readonly ITopicMessageRepository _topicMessageRepository;

        public ChatProcessor(IConfiguration configuration,
            ILogMessageRepository logMessageRepository,
            ITopicMessageRepository topicMessageRepository)
        {
            _apiKey = configuration["ThaiLLMAPI:Key"] ?? throw new InvalidOperationException("ThaiLLMAPI:Key is not configured");
            _logMessageRepository = logMessageRepository;
            _topicMessageRepository = topicMessageRepository;
        }

        public async Task<string> EnsureTopicAsync(ThaiLLMRequestModel req)
        {
            if (!string.IsNullOrEmpty(req.topicId))
            {
                return req.topicId;
            }

            // First user message becomes the new topic name placeholder.
            var firstUserMessage = req.Messages.FirstOrDefault(m => m.Role == "user")?.Content
                ?? req.Messages.First().Content;
            return await TopicLogMessageSave(firstUserMessage);
        }

        public async IAsyncEnumerable<string> StreamChatMessageAsync(ThaiLLMRequestModel req, string model, string topicId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string baseUrl = $"http://thaillm.or.th/api/{model}/v1/chat/completions";

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _apiKey);

            // OpenAI-style streaming omits the usage block by default; opt in so the LLM
            // emits a final `usage` chunk just before [DONE] with prompt/completion totals.
            req.StreamOptions = new StreamOptions { IncludeUsage = true };

            var snakeCaseOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };
            string jsonBody = JsonSerializer.Serialize(req, snakeCaseOptions);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"API returned {(int)response.StatusCode}: {errorBody}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            int promptTokens = 0, completionTokens = 0, totalTokens = 0;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line != null && line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            var streamContent = contentProp.GetString();
                            if (!string.IsNullOrEmpty(streamContent))
                            {
                                fullResponse.Append(streamContent);
                                yield return streamContent;
                            }
                        }
                    }

                    // The usage chunk arrives once, just before [DONE], with `choices: []`
                    // and a populated `usage` object (see response_llm.txt for the shape).
                    if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                    {
                        if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.ValueKind == JsonValueKind.Number)
                            promptTokens = pt.GetInt32();
                        if (usage.TryGetProperty("completion_tokens", out var ct) && ct.ValueKind == JsonValueKind.Number)
                            completionTokens = ct.GetInt32();
                        if (usage.TryGetProperty("total_tokens", out var tt) && tt.ValueKind == JsonValueKind.Number)
                            totalTokens = tt.GetInt32();
                    }
                }
            }

            // Signal frontend that content streaming is complete
            yield return "[DONE]";

            if (fullResponse.Length > 0)
            {
                var userMessageOrder = req.Messages.Length;
                var botMessageOrder = userMessageOrder + 1;

                await Task.WhenAll(
                    LogMessageSave(req.Messages.Last().Content, "user", topicId, userMessageOrder),
                    LogMessageSave(fullResponse.ToString(), "assistant", topicId, botMessageOrder, promptTokens, completionTokens, totalTokens)
                );
            }
        }
        public async Task<TopicMessage[]> GetAllHistoryMessage()
        {
            var topics = await _topicMessageRepository.GetAllAsync();
            return topics
                .Where(tm => tm.is_active)
                .OrderByDescending(tm => tm.created_at)
                .ThenByDescending(tm => tm.updated_at)
                .Select(tm => new TopicMessage
                {
                    Id = tm.id,
                    TopicName = tm.topic_name,
                    CreatedAt = tm.created_at,
                    UpdatedAt = tm.updated_at,
                })
                .ToArray();
        }

        public async Task<HistoryLLMModel> GetHistoryMessage(string topicId)
        {
            try
            {
                var topic = await _topicMessageRepository.GetByIdAsync(topicId);
                if (topic != null)
                {
                    var logsReponse = await _logMessageRepository.GetByTopicIdAsync(topic.id);
                    HistoryLLMModel logsHistory = new HistoryLLMModel
                    {
                        Messages = logsReponse.OrderBy(l => l.created_at).Select(l => new MessageJson
                        {
                            Role = l.role,
                            Content = l.content
                        }).ToArray()
                    };
                    return logsHistory;
                }
                else
                {
                    return new HistoryLLMModel();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving history messages for topicId {topicId}: {ex.Message}", ex);
            }
        }

        public async Task<string> TopicLogMessageSave(string message)
        {
            var result = await _topicMessageRepository.InsertAsync(new Model.PostgreSQL.topic_message
            {
                topic_name = message,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                is_active = true
            });
            return result.id;
        }
        public async Task LogMessageSave(string content, string role, string topicId, int order, int promptToken = 0, int completionToken = 0, int totalToken = 0)
        {
            await _logMessageRepository.InsertAsync(new Model.PostgreSQL.log_message
            {
                content = content,
                role = role,
                topic_id = topicId,
                order = order,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                is_active = true,
                prompt_tokens = promptToken,
                completion_tokens = completionToken,
                total_tokens = totalToken
            });
        }
        public async Task<bool> UpdateTopicName(string topicId, string name)
        {
            try
            {
                topic_message topic = await _topicMessageRepository.GetByIdAsync(topicId);
                topic.topic_name = name;
                await _topicMessageRepository.UpdateAsync(topic);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating topic name for topicId {topicId}: {ex.Message}", ex);
            }
        }
        public async Task<bool> DeleteTopic(string topicId)
        {
            try
            {
                topic_message topic = await _topicMessageRepository.GetByIdAsync(topicId);
                if (topic == null) return false;

                topic.is_active = false;
                topic.updated_at = DateTime.UtcNow;
                await _topicMessageRepository.UpdateAsync(topic);

                var logs = await _logMessageRepository.GetByTopicIdAsync(topicId);
                foreach (var log in logs)
                {
                    log.is_active = false;
                    log.updated_at = DateTime.UtcNow;
                    await _logMessageRepository.UpdateAsync(log);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting topic for topicId {topicId}: {ex.Message}", ex);
            }
        }
    }
}
