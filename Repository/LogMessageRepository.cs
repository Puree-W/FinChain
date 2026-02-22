using FinChain.Model.PostgreSQL;
using Supabase.Postgrest;

namespace FinChain.Repository
{
    public class LogMessageRepository : SupabaseRepository<log_message>, ILogMessageRepository
    {
        public LogMessageRepository(Supabase.Client supabase) : base(supabase) { }

        public async Task<List<log_message>> GetByTopicIdAsync(string topicId)
        {
            var response = await _supabase.From<log_message>()
                .Filter("topic_id", Constants.Operator.Equals, topicId)
                .Order("created_at", Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }
    }
}
