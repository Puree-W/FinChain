using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinChain.Model.PostgreSQL
{
    [Table("embedding_config")]
    public class embedding_config : BaseModel
    {
        [PrimaryKey("id", false)]
        public long id { get; set; }
        public string name { get; set; } = default!;
        public string endpoint { get; set; } = default!;
        public string? json_request { get; set; }
        public string? api_key { get; set; }
        public string? auth_header_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool active_flag { get; set; }
    }
}
