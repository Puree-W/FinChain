using FinChain.Model.PostgreSQL;

namespace FinChain.Repository
{
    public interface IModelTemplateRepository : ISupabaseRepository<model_template>
    {
        Task<List<model_template>> GetActiveAsync();
        Task<model_template?> GetDefaultAsync();
        Task ClearDefaultsExceptAsync(string keepId);
    }
}
