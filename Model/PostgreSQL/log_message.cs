using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinChain.Model.PostgreSQL
{
    [Table("log_message")]
    public class log_message : BaseModel
    {
        [PrimaryKey]
        public string id { get; set; }
        public string content { get; set; }
        public string role { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string topic_id { get; set; }
        public bool is_active { get; set; }
        public int order { get; set; }
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}
