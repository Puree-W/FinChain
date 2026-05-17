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
        Task<string> EnsureTopicAsync(ChatRequestModel req);
        IAsyncEnumerable<string> StreamChatMessageAsync(ChatRequestModel req, string topicId, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        Task<HistoryLLMModel> GetHistoryMessage(string topicId);
        Task<TopicMessage[]> GetAllHistoryMessage();
        Task<string> TopicLogMessageSave(string message);
        Task LogMessageSave(string content, string role, string topicId, int order, int promptToken = 0, int completionToken = 0, int totalToken = 0);
        Task<bool> UpdateTopicName(string topicId, string name);
        Task<bool> DeleteTopic(string topicId);
    }

    public class ChatProcessor : IChatProcessor
    {
        private readonly ILogMessageRepository _logMessageRepository;
        private readonly ITopicMessageRepository _topicMessageRepository;
        private readonly IModelTemplateRepository _templateRepository;
        private readonly IAiConfigRepository _aiConfigRepository;

        public ChatProcessor(
            ILogMessageRepository logMessageRepository,
            ITopicMessageRepository topicMessageRepository,
            IModelTemplateRepository templateRepository,
            IAiConfigRepository aiConfigRepository)
        {
            _logMessageRepository = logMessageRepository;
            _topicMessageRepository = topicMessageRepository;
            _templateRepository = templateRepository;
            _aiConfigRepository = aiConfigRepository;
        }

        public async Task<string> EnsureTopicAsync(ChatRequestModel req)
        {
            if (!string.IsNullOrEmpty(req.TopicId))
            {
                return req.TopicId;
            }

            // First user message becomes the new topic name placeholder.
            var firstUserMessage = req.Messages.FirstOrDefault(m => m.Role == "user")?.Content
                ?? req.Messages.First().Content;
            return await TopicLogMessageSave(firstUserMessage);
        }

        public async IAsyncEnumerable<string> StreamChatMessageAsync(ChatRequestModel req, string topicId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Resolve the template + endpoint up front. Errors here short-circuit before any
            // SSE bytes are written, so the controller can surface them as JSON.
            model_template? template = null;
            if (!string.IsNullOrEmpty(req.TemplateId))
            {
                template = await _templateRepository.GetByIdAsync(req.TemplateId);
            }
            template ??= await _templateRepository.GetDefaultAsync();
            if (template == null)
            {
                throw new InvalidOperationException("No model template selected and no default template is configured. Open Configuration → Model Setting to create one.");
            }

            var aiConfig = await _aiConfigRepository.GetByLongIdAsync(template.ai_config_id);
            if (aiConfig == null)
            {
                throw new InvalidOperationException($"Template '{template.name}' references a missing LLM configuration (id {template.ai_config_id}).");
            }
            if (!aiConfig.active_flag)
            {
                throw new InvalidOperationException($"Template '{template.name}' is pointed at a disabled LLM endpoint '{aiConfig.name}'. Re-enable it in Configuration → LLM Configuration, or repoint the template.");
            }
            if (string.IsNullOrEmpty(aiConfig.endpoint))
            {
                throw new InvalidOperationException($"LLM endpoint '{aiConfig.name}' has no URL configured.");
            }

            // Optionally prepend the template's system prompt — but never duplicate one the
            // caller already supplied.
            var outboundMessages = req.Messages;
            if (!string.IsNullOrWhiteSpace(template.system_prompt)
                && (outboundMessages.Length == 0 || outboundMessages[0].Role != "system"))
            {
                outboundMessages = new[] { new MessageJson { Role = "system", Content = template.system_prompt } }
                    .Concat(outboundMessages)
                    .ToArray();
            }

            // api_shape only governs the wire-format details we cannot infer from json_request:
            //   - response parsing (Chat Completions emits `choices[].delta.content`;
            //     Responses emits typed `response.output_text.delta` events)
            //   - the conversation key fallback (`messages` vs `input`) when json_request
            //     doesn't make it explicit
            // Parameter naming (max_tokens vs max_output_tokens, whether stream_options is
            // sent, etc.) is NOT inferred here — json_request on ai_config is the source of
            // truth. Slider values fill in the keys the user already wrote.
            var shape = string.IsNullOrWhiteSpace(aiConfig.api_shape) ? "chat_completions" : aiConfig.api_shape;
            var isResponses = shape == "responses";

            // Body precedence (low → high):
            //   1. json_request — canonical body for this endpoint, including the provider's
            //                     parameter keys (e.g. `max_output_tokens` for Responses,
            //                     `max_tokens` for Chat Completions). Whatever keys the user
            //                     put here decide what gets sent.
            //   2. template sliders — fill in values for slider keys that ALREADY EXIST in
            //                         json_request. We don't inject keys the user didn't write,
            //                         so endpoints that reject unknown params (e.g. GPT-5.4
            //                         rejecting `stream_options.include_usage`) stay clean.
            //   3. messages/input + stream — always derived from the live conversation.
            if (string.IsNullOrWhiteSpace(aiConfig.json_request))
            {
                throw new InvalidOperationException($"LLM endpoint '{aiConfig.name}' has no json_request template configured. Add a request body in Configuration → LLM Configuration with the provider's parameter keys (e.g. `max_tokens` for Chat Completions or `max_output_tokens` for Responses).");
            }

            Dictionary<string, object?> body;
            try
            {
                body = JsonSerializer.Deserialize<Dictionary<string, object?>>(aiConfig.json_request)
                       ?? new Dictionary<string, object?>();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"LLM endpoint '{aiConfig.name}' has an invalid json_request template: {ex.Message}");
            }

            // Slider → key candidates. Each slider is tried against the keys the provider
            // might use; the first candidate already present in json_request gets the
            // template's value. Sliders whose keys aren't in json_request are skipped.
            ApplyTemplateValue(body, template.temperature,       "temperature");
            ApplyTemplateValue(body, template.top_p,             "top_p");
            ApplyTemplateValue(body, template.max_tokens,        "max_tokens", "max_output_tokens");
            ApplyTemplateValue(body, template.frequency_penalty, "frequency_penalty");
            ApplyTemplateValue(body, template.presence_penalty,  "presence_penalty");

            // Conversation + stream are non-negotiable — always overwrite. If the user spelled
            // out either `messages` or `input` in json_request we respect that choice; otherwise
            // fall back to the api_shape default.
            var conversationKey = body.ContainsKey("input") ? "input"
                                : body.ContainsKey("messages") ? "messages"
                                : (isResponses ? "input" : "messages");
            body[conversationKey] = outboundMessages.Select(m => new { role = m.Role, content = m.Content }).ToArray();
            body["stream"] = true;

            var jsonBody = JsonSerializer.Serialize(body);

            using var client = new HttpClient();
            var authHeader = string.IsNullOrWhiteSpace(aiConfig.auth_header_name) ? "api-key" : aiConfig.auth_header_name;
            if (!string.IsNullOrEmpty(aiConfig.api_key))
            {
                client.DefaultRequestHeaders.Add(authHeader, aiConfig.api_key);
            }

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, aiConfig.endpoint) { Content = content };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"LLM endpoint '{aiConfig.name}' returned {(int)response.StatusCode}: {errorBody}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullResponse = new StringBuilder();
            int promptTokens = 0, completionTokens = 0, totalTokens = 0;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null || !line.StartsWith("data: ")) continue;
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                JsonDocument? doc = null;
                try { doc = JsonDocument.Parse(data); }
                catch (JsonException) { continue; } // skip malformed chunks rather than killing the whole stream

                using (doc)
                {
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object) continue;

                    // Parse both wire formats in one pass — whichever fields are present win.
                    // Keeps the stream readable even if api_shape is misconfigured (e.g. the
                    // endpoint was registered as chat_completions but actually returns the
                    // Responses event format, which is what GPT-5.4 does).

                    // Chat Completions: choices[0].delta.content
                    if (root.TryGetProperty("choices", out var choices)
                        && choices.ValueKind == JsonValueKind.Array
                        && choices.GetArrayLength() > 0
                        && choices[0].TryGetProperty("delta", out var delta)
                        && delta.TryGetProperty("content", out var contentProp))
                    {
                        var streamContent = contentProp.GetString();
                        if (!string.IsNullOrEmpty(streamContent))
                        {
                            fullResponse.Append(streamContent);
                            yield return streamContent;
                        }
                    }

                    // Responses API: typed events with `delta` on response.output_text.delta
                    if (root.TryGetProperty("type", out var typeProp))
                    {
                        var type = typeProp.GetString();
                        if (type == "response.output_text.delta"
                            && root.TryGetProperty("delta", out var deltaProp)
                            && deltaProp.ValueKind == JsonValueKind.String)
                        {
                            var streamContent = deltaProp.GetString();
                            if (!string.IsNullOrEmpty(streamContent))
                            {
                                fullResponse.Append(streamContent);
                                yield return streamContent;
                            }
                        }
                        else if (type == "response.completed"
                              && root.TryGetProperty("response", out var responseProp)
                              && responseProp.TryGetProperty("usage", out var rUsage)
                              && rUsage.ValueKind == JsonValueKind.Object)
                        {
                            ReadUsage(rUsage, "input_tokens", "output_tokens", ref promptTokens, ref completionTokens, ref totalTokens);
                        }
                    }

                    // Chat Completions usage chunk (sent when stream_options.include_usage is enabled)
                    if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                    {
                        ReadUsage(usage, "prompt_tokens", "completion_tokens", ref promptTokens, ref completionTokens, ref totalTokens);
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

        // Writes `value` into the first candidate key that already exists in `body`. If none
        // of the candidates are present, the slider is silently dropped — the user opted out
        // by not including the key in json_request.
        private static void ApplyTemplateValue(Dictionary<string, object?> body, object value, params string[] candidateKeys)
        {
            foreach (var key in candidateKeys)
            {
                if (body.ContainsKey(key))
                {
                    body[key] = value;
                    return;
                }
            }
        }

        // Reads provider-specific token-count fields off a usage object. `total_tokens` is the
        // same key across providers; only the prompt/completion names differ.
        private static void ReadUsage(JsonElement usage, string promptKey, string completionKey, ref int promptTokens, ref int completionTokens, ref int totalTokens)
        {
            if (usage.TryGetProperty(promptKey, out var pt) && pt.ValueKind == JsonValueKind.Number)
                promptTokens = pt.GetInt32();
            if (usage.TryGetProperty(completionKey, out var ct) && ct.ValueKind == JsonValueKind.Number)
                completionTokens = ct.GetInt32();
            if (usage.TryGetProperty("total_tokens", out var tt) && tt.ValueKind == JsonValueKind.Number)
                totalTokens = tt.GetInt32();
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
