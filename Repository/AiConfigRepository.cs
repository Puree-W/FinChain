using FinChain.Model.PostgreSQL;
using Supabase.Postgrest;

namespace FinChain.Repository
{
    public class AiConfigRepository : SupabaseRepository<ai_config>, IAiConfigRepository
    {
        public AiConfigRepository(Supabase.Client supabase) : base(supabase) { }

        public async Task<List<ai_config>> GetActiveAsync()
        {
            var response = await _supabase.From<ai_config>()
                .Filter("active_flag", Constants.Operator.Equals, "true")
                .Order("created_at", Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }

        public async Task<ai_config?> GetByLongIdAsync(long id)
        {
            var response = await _supabase.From<ai_config>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Single();
            return response;
        }
    }
}
