using FinChain.Model.PostgreSQL;

namespace FinChain.Repository
{
    public interface IEmbeddingConfigRepository : ISupabaseRepository<embedding_config>
    {
        Task<List<embedding_config>> GetActiveAsync();
        Task<embedding_config?> GetByLongIdAsync(long id);
    }
}
