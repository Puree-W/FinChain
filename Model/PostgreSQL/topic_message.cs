using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinChain.Model.PostgreSQL
{
    [Table("topic_message")]
    public class topic_message : BaseModel
    {
        [PrimaryKey]
        public string id { get; set; }
        public string topic_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool is_active { get; set; }
    }
}
