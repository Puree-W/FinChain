using Supabase.Postgrest.Models;
using System.Linq.Expressions;

namespace FinChain.Repository
{
    public interface ISupabaseRepository<T> where T : BaseModel, new()
    {
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task<T?> WhereAsync(Expression<Func<T, bool>> predicate);
        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(string id);
    }
}
