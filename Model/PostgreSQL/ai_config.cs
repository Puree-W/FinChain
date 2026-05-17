using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinChain.Model.PostgreSQL
{
    [Table("ai_config")]
    public class ai_config : BaseModel
    {
        [PrimaryKey("id", false)]
        public long id { get; set; }
        public string name { get; set; } = default!;
        public string endpoint { get; set; } = default!;
        // json_request is required end-to-end (validated server-side, enforced in the UI) — it
        // drives parameter naming for the outbound LLM body.
        public string json_request { get; set; } = default!;
        // The remaining strings can legitimately be NULL in the DB (either pre-Phase-2 rows
        // that predate the column, or genuinely empty for endpoints that don't need them).
        public string? api_key { get; set; }
        public string? auth_header_name { get; set; }
        public string? api_shape { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool active_flag { get; set; }
    }
}
