namespace FinChain.Model
{
    public class AiConfigModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string Endpoint { get; set; } = default!;
        public string? JsonRequest { get; set; }
        public string? AuthHeaderName { get; set; }
        public string? ApiShape { get; set; }
        public string? ApiKeyMasked { get; set; }
        public bool HasApiKey { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EmbeddingConfigModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string Endpoint { get; set; } = default!;
        public string? JsonRequest { get; set; }
        public string? AuthHeaderName { get; set; }
        public string? ApiKeyMasked { get; set; }
        public bool HasApiKey { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AiConfigUpsert
    {
        public string Name { get; set; } = default!;
        public string Endpoint { get; set; } = default!;
        // Required — the chat path needs at least `{ "model": "..." }` or equivalent
        // provider dispatch fields, so we enforce it server-side in ConfigurationProcessor.
        public string JsonRequest { get; set; } = default!;
        public string? AuthHeaderName { get; set; }
        // "chat_completions" or "responses". Null defaults to "chat_completions".
        public string? ApiShape { get; set; }
        // Null on PUT means "keep existing key"; required on POST.
        public string? ApiKey { get; set; }
        public bool? ActiveFlag { get; set; }
    }

    public class EmbeddingConfigUpsert
    {
        public string Name { get; set; } = default!;
        public string Endpoint { get; set; } = default!;
        public string JsonRequest { get; set; } = default!;
        public string? AuthHeaderName { get; set; }
        public string? ApiKey { get; set; }
        public bool? ActiveFlag { get; set; }
    }

    public class ModelTemplateModel
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public long AiConfigId { get; set; }
        public string? AiConfigName { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public float TopP { get; set; }
        public float FrequencyPenalty { get; set; }
        public float PresencePenalty { get; set; }
        public string? SystemPrompt { get; set; }
        public bool IsDefault { get; set; }
        public bool ActiveFlag { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ModelTemplateUpsert
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public long AiConfigId { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public float TopP { get; set; }
        public float FrequencyPenalty { get; set; }
        public float PresencePenalty { get; set; }
        public string? SystemPrompt { get; set; }
        public bool IsDefault { get; set; }
        public bool? ActiveFlag { get; set; }
    }
}
