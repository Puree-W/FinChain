using FinChain.Model.PostgreSQL;
using Supabase.Postgrest;

namespace FinChain.Repository
{
    public class ModelTemplateRepository : SupabaseRepository<model_template>, IModelTemplateRepository
    {
        public ModelTemplateRepository(Supabase.Client supabase) : base(supabase) { }

        public async Task<List<model_template>> GetActiveAsync()
        {
            var response = await _supabase.From<model_template>()
                .Filter("active_flag", Constants.Operator.Equals, "true")
                .Order("created_at", Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }

        public async Task<model_template?> GetDefaultAsync()
        {
            var response = await _supabase.From<model_template>()
                .Filter("is_default", Constants.Operator.Equals, "true")
                .Filter("active_flag", Constants.Operator.Equals, "true")
                .Limit(1)
                .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task ClearDefaultsExceptAsync(string keepId)
        {
            // Pull all rows currently flagged default; unflag everything except the one we want kept.
            var response = await _supabase.From<model_template>()
                .Filter("is_default", Constants.Operator.Equals, "true")
                .Get();
            foreach (var row in response.Models)
            {
                if (row.id == keepId) continue;
                row.is_default = false;
                row.updated_at = DateTime.UtcNow;
                await _supabase.From<model_template>().Update(row);
            }
        }
    }
}
