using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinChain.Model.PostgreSQL
{
    [Table("model_template")]
    public class model_template : BaseModel
    {
        [PrimaryKey]
        public string id { get; set; } = default!;
        public string name { get; set; } = default!;
        // description and system_prompt are user-optional — table allows NULL,
        // C# must too or System.Text.Json balks when the row comes back with null.
        public string? description { get; set; }
        public long ai_config_id { get; set; }
        public float temperature { get; set; }
        public int max_tokens { get; set; }
        public float top_p { get; set; }
        public float frequency_penalty { get; set; }
        public float presence_penalty { get; set; }
        public string? system_prompt { get; set; }
        public bool is_default { get; set; }
        public bool active_flag { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
