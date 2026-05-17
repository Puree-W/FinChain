using FinChain.Model.PostgreSQL;

namespace FinChain.Repository
{
    public interface IAiConfigRepository : ISupabaseRepository<ai_config>
    {
        Task<List<ai_config>> GetActiveAsync();
        Task<ai_config?> GetByLongIdAsync(long id);
    }
}
