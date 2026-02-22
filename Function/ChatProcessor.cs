using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FinChain.Model;
using FinChain.Repository;

namespace FinChain.Function
{
    public interface IChatProcessor
    {
        IAsyncEnumerable<string> StreamChatMessageAsync(ThaiLLMRequestModel req, string model, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        Task<HistoryLLMModel> GetHistoryMessage(string topicId);
        Task<TopicMessage[]> GetAllHistoryMessage();
        Task<string> TopicLogMessageSave(string message);
        Task LogMessageSave(string content, string role ,string topicId, int order, int promptToken = 0, int completionToken = 0, int totalToken = 0);
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

        public async IAsyncEnumerable<string> StreamChatMessageAsync(ThaiLLMRequestModel req, string model, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string topicId;

            if (String.IsNullOrEmpty(req.topicId))
            {
                topicId = await TopicLogMessageSave(req.Messages[0].Content);
            }
            else 
            {
                topicId = req.topicId;
            }

            string baseUrl = $"http://thaillm.or.th/api/{model}/v1/chat/completions";

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _apiKey);

            var snakeCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
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

                    if (root.TryGetProperty("usage", out var usage))
                    {
                        promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                        completionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
                        totalTokens = usage.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0;
                    }
                }
            }

            if (fullResponse.Length > 0)
            {

                var userMessageOrder = req.Messages.Length;
                var botMessageOrder = userMessageOrder + 1;

                await Task.WhenAll(
                    LogMessageSave(req.Messages.Last().Content, "U", topicId, userMessageOrder),
                    LogMessageSave(fullResponse.ToString(), "B", topicId, botMessageOrder, promptTokens, completionTokens, totalTokens)
                );
            }
        }
        public Task<TopicMessage[]> GetAllHistoryMessage()
        {
            return _topicMessageRepository.GetAllAsync().ContinueWith(t =>
            {
                return t.Result
                    .OrderByDescending(tm => tm.created_at)
                    .Select(tm => new TopicMessage
                    {
                        Id = tm.id,
                        TopicName = tm.topic_name,
                        CreatedAt = tm.created_at,
                        UpdatedAt = tm.updated_at,
                    })
                    .ToArray();
            });
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

    }
}
