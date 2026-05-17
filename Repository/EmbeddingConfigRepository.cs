using FinChain.Model.PostgreSQL;
using Supabase.Postgrest;

namespace FinChain.Repository
{
    public class EmbeddingConfigRepository : SupabaseRepository<embedding_config>, IEmbeddingConfigRepository
    {
        public EmbeddingConfigRepository(Supabase.Client supabase) : base(supabase) { }

        public async Task<List<embedding_config>> GetActiveAsync()
        {
            var response = await _supabase.From<embedding_config>()
                .Filter("active_flag", Constants.Operator.Equals, "true")
                .Order("created_at", Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }

        public async Task<embedding_config?> GetByLongIdAsync(long id)
        {
            var response = await _supabase.From<embedding_config>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Single();
            return response;
        }
    }
}
