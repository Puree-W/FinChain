using FinChain.Model.PostgreSQL;
using Supabase.Postgrest;

namespace FinChain.Repository
{
    public class TopicMessageRepository : SupabaseRepository<topic_message>, ITopicMessageRepository
    {
        public TopicMessageRepository(Supabase.Client supabase) : base(supabase) { }

        public async Task<List<topic_message>> GetActiveTopicsAsync()
        {
            var response = await _supabase.From<topic_message>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Order("create_at", Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }
    }
}
