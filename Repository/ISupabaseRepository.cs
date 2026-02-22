using Supabase.Postgrest.Models;

namespace FinChain.Repository
{
    public interface ISupabaseRepository<T> where T : BaseModel, new()
    {
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(string id);
    }
}
