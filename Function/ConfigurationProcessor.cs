using FinChain.Model;
using FinChain.Model.PostgreSQL;
using FinChain.Repository;

namespace FinChain.Function
{
    public interface IConfigurationProcessor
    {
        Task<AiConfigModel[]> GetAiConfigsAsync();
        Task<AiConfigModel?> GetAiConfigAsync(long id);
        Task<AiConfigModel> CreateAiConfigAsync(AiConfigUpsert payload);
        Task<AiConfigModel> UpdateAiConfigAsync(long id, AiConfigUpsert payload);
        Task<bool> DeleteAiConfigAsync(long id);

        Task<EmbeddingConfigModel[]> GetEmbeddingConfigsAsync();
        Task<EmbeddingConfigModel?> GetEmbeddingConfigAsync(long id);
        Task<EmbeddingConfigModel> CreateEmbeddingConfigAsync(EmbeddingConfigUpsert payload);
        Task<EmbeddingConfigModel> UpdateEmbeddingConfigAsync(long id, EmbeddingConfigUpsert payload);
        Task<bool> DeleteEmbeddingConfigAsync(long id);

        Task<ModelTemplateModel[]> GetModelTemplatesAsync();
        Task<ModelTemplateModel?> GetModelTemplateAsync(string id);
        Task<ModelTemplateModel?> GetDefaultModelTemplateAsync();
        Task<ModelTemplateModel> CreateModelTemplateAsync(ModelTemplateUpsert payload);
        Task<ModelTemplateModel> UpdateModelTemplateAsync(string id, ModelTemplateUpsert payload);
        Task<bool> SetDefaultTemplateAsync(string id);
        Task<bool> DeleteModelTemplateAsync(string id);
    }

    public class ConfigurationProcessor : IConfigurationProcessor
    {
        private readonly IAiConfigRepository _aiConfigRepo;
        private readonly IEmbeddingConfigRepository _embeddingConfigRepo;
        private readonly IModelTemplateRepository _templateRepo;

        public ConfigurationProcessor(
            IAiConfigRepository aiConfigRepo,
            IEmbeddingConfigRepository embeddingConfigRepo,
            IModelTemplateRepository templateRepo)
        {
            _aiConfigRepo = aiConfigRepo;
            _embeddingConfigRepo = embeddingConfigRepo;
            _templateRepo = templateRepo;
        }

        // ─── AI config ───
        public async Task<AiConfigModel[]> GetAiConfigsAsync()
        {
            var rows = await _aiConfigRepo.GetAllAsync();
            return rows.OrderByDescending(r => r.created_at).Select(ToDto).ToArray();
        }

        public async Task<AiConfigModel?> GetAiConfigAsync(long id)
        {
            var row = await _aiConfigRepo.GetByLongIdAsync(id);
            return row == null ? null : ToDto(row);
        }

        public async Task<AiConfigModel> CreateAiConfigAsync(AiConfigUpsert payload)
        {
            ValidateAiConfigUpsert(payload);
            var row = new ai_config
            {
                name = payload.Name,
                endpoint = payload.Endpoint,
                json_request = payload.JsonRequest,
                auth_header_name = string.IsNullOrWhiteSpace(payload.AuthHeaderName) ? "api-key" : payload.AuthHeaderName,
                api_shape = NormalizeShape(payload.ApiShape),
                api_key = payload.ApiKey,
                active_flag = payload.ActiveFlag ?? true,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
            };
            var inserted = await _aiConfigRepo.InsertAsync(row);
            return ToDto(inserted);
        }

        public async Task<AiConfigModel> UpdateAiConfigAsync(long id, AiConfigUpsert payload)
        {
            ValidateAiConfigUpsert(payload);
            var row = await _aiConfigRepo.GetByLongIdAsync(id)
                ?? throw new Exception($"ai_config with id {id} not found");

            row.name = payload.Name;
            row.endpoint = payload.Endpoint;
            row.json_request = payload.JsonRequest;
            row.auth_header_name = string.IsNullOrWhiteSpace(payload.AuthHeaderName) ? "api-key" : payload.AuthHeaderName;
            if (!string.IsNullOrWhiteSpace(payload.ApiShape)) row.api_shape = NormalizeShape(payload.ApiShape);
            // Null api_key on PUT means "keep existing" — prevents an accidental wipe when the user
            // edits without retyping the key in the masked field.
            if (!string.IsNullOrEmpty(payload.ApiKey)) row.api_key = payload.ApiKey;
            if (payload.ActiveFlag.HasValue) row.active_flag = payload.ActiveFlag.Value;
            row.updated_at = DateTime.UtcNow;

            var updated = await _aiConfigRepo.UpdateAsync(row);
            return ToDto(updated);
        }

        // The chat path needs at least `{ "model": "..." }` (or equivalent provider dispatch
        // fields) to build a valid request body. Without it the LLM rejects the call.
        private static void ValidateAiConfigUpsert(AiConfigUpsert payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Name))
                throw new InvalidOperationException("Name is required.");
            if (string.IsNullOrWhiteSpace(payload.Endpoint))
                throw new InvalidOperationException("Endpoint URL is required.");
            if (string.IsNullOrWhiteSpace(payload.JsonRequest))
                throw new InvalidOperationException("JSON request template is required (e.g. \"{ \\\"model\\\": \\\"gpt-4o\\\" }\").");
        }

        // Only two shapes are valid; anything else falls back to the OpenAI Chat Completions shape.
        private static string NormalizeShape(string? shape)
        {
            return shape == "responses" ? "responses" : "chat_completions";
        }

        public async Task<bool> DeleteAiConfigAsync(long id)
        {
            var row = await _aiConfigRepo.GetByLongIdAsync(id);
            if (row == null) return false;
            row.active_flag = false;
            row.updated_at = DateTime.UtcNow;
            await _aiConfigRepo.UpdateAsync(row);
            return true;
        }

        // ─── Embedding config ───
        public async Task<EmbeddingConfigModel[]> GetEmbeddingConfigsAsync()
        {
            var rows = await _embeddingConfigRepo.GetAllAsync();
            return rows.OrderByDescending(r => r.created_at).Select(ToDto).ToArray();
        }

        public async Task<EmbeddingConfigModel?> GetEmbeddingConfigAsync(long id)
        {
            var row = await _embeddingConfigRepo.GetByLongIdAsync(id);
            return row == null ? null : ToDto(row);
        }

        public async Task<EmbeddingConfigModel> CreateEmbeddingConfigAsync(EmbeddingConfigUpsert payload)
        {
            var row = new embedding_config
            {
                name = payload.Name,
                endpoint = payload.Endpoint,
                json_request = payload.JsonRequest,
                auth_header_name = string.IsNullOrWhiteSpace(payload.AuthHeaderName) ? "api-key" : payload.AuthHeaderName,
                api_key = payload.ApiKey,
                active_flag = payload.ActiveFlag ?? true,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
            };
            var inserted = await _embeddingConfigRepo.InsertAsync(row);
            return ToDto(inserted);
        }

        public async Task<EmbeddingConfigModel> UpdateEmbeddingConfigAsync(long id, EmbeddingConfigUpsert payload)
        {
            var row = await _embeddingConfigRepo.GetByLongIdAsync(id)
                ?? throw new Exception($"embedding_config with id {id} not found");

            row.name = payload.Name;
            row.endpoint = payload.Endpoint;
            row.json_request = payload.JsonRequest;
            row.auth_header_name = string.IsNullOrWhiteSpace(payload.AuthHeaderName) ? "api-key" : payload.AuthHeaderName;
            if (!string.IsNullOrEmpty(payload.ApiKey)) row.api_key = payload.ApiKey;
            if (payload.ActiveFlag.HasValue) row.active_flag = payload.ActiveFlag.Value;
            row.updated_at = DateTime.UtcNow;

            var updated = await _embeddingConfigRepo.UpdateAsync(row);
            return ToDto(updated);
        }

        public async Task<bool> DeleteEmbeddingConfigAsync(long id)
        {
            var row = await _embeddingConfigRepo.GetByLongIdAsync(id);
            if (row == null) return false;
            row.active_flag = false;
            row.updated_at = DateTime.UtcNow;
            await _embeddingConfigRepo.UpdateAsync(row);
            return true;
        }

        // ─── Model template ───
        public async Task<ModelTemplateModel[]> GetModelTemplatesAsync()
        {
            var rows = await _templateRepo.GetAllAsync();
            var aiConfigs = await _aiConfigRepo.GetAllAsync();
            var nameLookup = aiConfigs.ToDictionary(c => c.id, c => c.name);
            return rows
                .OrderByDescending(r => r.is_default)
                .ThenByDescending(r => r.created_at)
                .Select(r => ToDto(r, nameLookup.GetValueOrDefault(r.ai_config_id)))
                .ToArray();
        }

        public async Task<ModelTemplateModel?> GetModelTemplateAsync(string id)
        {
            var row = await _templateRepo.GetByIdAsync(id);
            if (row == null) return null;
            var ai = await _aiConfigRepo.GetByLongIdAsync(row.ai_config_id);
            return ToDto(row, ai?.name);
        }

        public async Task<ModelTemplateModel?> GetDefaultModelTemplateAsync()
        {
            var row = await _templateRepo.GetDefaultAsync();
            if (row == null) return null;
            var ai = await _aiConfigRepo.GetByLongIdAsync(row.ai_config_id);
            return ToDto(row, ai?.name);
        }

        public async Task<ModelTemplateModel> CreateModelTemplateAsync(ModelTemplateUpsert payload)
        {
            ValidateModelTemplateUpsert(payload);
            var row = new model_template
            {
                name = payload.Name,
                description = NullIfBlank(payload.Description),
                ai_config_id = payload.AiConfigId,
                temperature = payload.Temperature,
                max_tokens = payload.MaxTokens,
                top_p = payload.TopP,
                frequency_penalty = payload.FrequencyPenalty,
                presence_penalty = payload.PresencePenalty,
                system_prompt = NullIfBlank(payload.SystemPrompt),
                is_default = payload.IsDefault,
                active_flag = payload.ActiveFlag ?? true,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
            };
            var inserted = await _templateRepo.InsertAsync(row);
            if (inserted.is_default)
            {
                await _templateRepo.ClearDefaultsExceptAsync(inserted.id);
            }
            var ai = await _aiConfigRepo.GetByLongIdAsync(inserted.ai_config_id);
            return ToDto(inserted, ai?.name);
        }

        public async Task<ModelTemplateModel> UpdateModelTemplateAsync(string id, ModelTemplateUpsert payload)
        {
            ValidateModelTemplateUpsert(payload);
            var row = await _templateRepo.GetByIdAsync(id)
                ?? throw new Exception($"model_template with id {id} not found");

            row.name = payload.Name;
            row.description = NullIfBlank(payload.Description);
            row.ai_config_id = payload.AiConfigId;
            row.temperature = payload.Temperature;
            row.max_tokens = payload.MaxTokens;
            row.top_p = payload.TopP;
            row.frequency_penalty = payload.FrequencyPenalty;
            row.presence_penalty = payload.PresencePenalty;
            row.system_prompt = NullIfBlank(payload.SystemPrompt);
            row.is_default = payload.IsDefault;
            if (payload.ActiveFlag.HasValue) row.active_flag = payload.ActiveFlag.Value;
            row.updated_at = DateTime.UtcNow;

            var updated = await _templateRepo.UpdateAsync(row);
            if (updated.is_default)
            {
                await _templateRepo.ClearDefaultsExceptAsync(updated.id);
            }
            var ai = await _aiConfigRepo.GetByLongIdAsync(updated.ai_config_id);
            return ToDto(updated, ai?.name);
        }

        private static void ValidateModelTemplateUpsert(ModelTemplateUpsert payload)
        {
            if (string.IsNullOrWhiteSpace(payload.Name))
                throw new InvalidOperationException("Template name is required.");
            if (payload.AiConfigId <= 0)
                throw new InvalidOperationException("An LLM endpoint must be selected.");
            // Description and SystemPrompt are intentionally optional.
        }

        // Empty/whitespace strings become NULL so the DB row stays clean and IS NULL filters work.
        private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

        public async Task<bool> SetDefaultTemplateAsync(string id)
        {
            var row = await _templateRepo.GetByIdAsync(id);
            if (row == null) return false;
            row.is_default = true;
            row.updated_at = DateTime.UtcNow;
            await _templateRepo.UpdateAsync(row);
            await _templateRepo.ClearDefaultsExceptAsync(id);
            return true;
        }

        public async Task<bool> DeleteModelTemplateAsync(string id)
        {
            var row = await _templateRepo.GetByIdAsync(id);
            if (row == null) return false;
            row.active_flag = false;
            row.is_default = false;
            row.updated_at = DateTime.UtcNow;
            await _templateRepo.UpdateAsync(row);
            return true;
        }

        // ─── Mappers ───
        // Masks the secret so the frontend can show a "you have a key set" affordance
        // without ever receiving the raw value back.
        private static string? MaskKey(string? key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.Length <= 4) return new string('•', key.Length);
            return new string('•', 4) + key[^4..];
        }

        private static AiConfigModel ToDto(ai_config r) => new()
        {
            Id = r.id,
            Name = r.name,
            Endpoint = r.endpoint,
            JsonRequest = r.json_request,
            AuthHeaderName = r.auth_header_name,
            ApiShape = string.IsNullOrWhiteSpace(r.api_shape) ? "chat_completions" : r.api_shape,
            ApiKeyMasked = MaskKey(r.api_key),
            HasApiKey = !string.IsNullOrEmpty(r.api_key),
            ActiveFlag = r.active_flag,
            CreatedAt = r.created_at,
            UpdatedAt = r.updated_at,
        };

        private static EmbeddingConfigModel ToDto(embedding_config r) => new()
        {
            Id = r.id,
            Name = r.name,
            Endpoint = r.endpoint,
            JsonRequest = r.json_request,
            AuthHeaderName = r.auth_header_name,
            ApiKeyMasked = MaskKey(r.api_key),
            HasApiKey = !string.IsNullOrEmpty(r.api_key),
            ActiveFlag = r.active_flag,
            CreatedAt = r.created_at,
            UpdatedAt = r.updated_at,
        };

        private static ModelTemplateModel ToDto(model_template r, string? aiConfigName) => new()
        {
            Id = r.id,
            Name = r.name,
            Description = r.description,
            AiConfigId = r.ai_config_id,
            AiConfigName = aiConfigName,
            Temperature = r.temperature,
            MaxTokens = r.max_tokens,
            TopP = r.top_p,
            FrequencyPenalty = r.frequency_penalty,
            PresencePenalty = r.presence_penalty,
            SystemPrompt = r.system_prompt,
            IsDefault = r.is_default,
            ActiveFlag = r.active_flag,
            CreatedAt = r.created_at,
            UpdatedAt = r.updated_at,
        };
    }
}
