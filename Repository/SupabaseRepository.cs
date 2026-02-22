using Supabase.Postgrest;
using Supabase.Postgrest.Models;

namespace FinChain.Repository
{
    public class SupabaseRepository<T> : ISupabaseRepository<T> where T : BaseModel, new()
    {
        protected readonly Supabase.Client _supabase;

        public SupabaseRepository(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<T>> GetAllAsync()
        {
            var response = await _supabase.From<T>().Get();
            return response.Models;
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var response = await _supabase.From<T>()
                .Filter("id", Constants.Operator.Equals, id)
                .Single();
            return response;
        }

        public async Task<T> InsertAsync(T entity)
        {
            var response = await _supabase.From<T>().Insert(entity);
            return response.Models.First();
        }

        public async Task<T> UpdateAsync(T entity)
        {
            var response = await _supabase.From<T>().Update(entity);
            return response.Models.First();
        }

        public async Task DeleteAsync(string id)
        {
            await _supabase.From<T>()
                .Filter("id", Constants.Operator.Equals, id)
                .Delete();
        }
    }
}
