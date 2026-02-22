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

    public class ThaiLLMRequestModel
    {
        public bool Stream { get; set; }
        public int MaxTokens { get; set; }
        public float Temperature { get; set; }
        public required MessageJson[] Messages { get; set; }
        public string? topicId {get;set;}
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
