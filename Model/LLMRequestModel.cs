namespace FinChain.Model
{   
    public class TopicMessage
    {
        public string Id { get; set; }
        public string TopicName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // Phase 2: the chat wire-format is just "what to send" — everything about
    // *how* to send it (provider URL, auth, model, sampling params, system prompt)
    // is resolved server-side from the template.
    public class ChatRequestModel
    {
        public required MessageJson[] Messages { get; set; }
        // Optional: when omitted, the backend falls back to the row with is_default = true.
        public string? TemplateId { get; set; }
        public string? TopicId { get; set; }
    }
    public class HistoryLLMModel
    {
        public MessageJson[] Messages { get; set; }
    }
    public class MessageJson
    {
        public required string Role { get; set; }
        public required string Content { get; set; }
    }
}
