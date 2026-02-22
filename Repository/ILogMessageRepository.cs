using FinChain.Model.PostgreSQL;

namespace FinChain.Repository
{
    public interface ILogMessageRepository : ISupabaseRepository<log_message>
    {
        Task<List<log_message>> GetByTopicIdAsync(string topicId);
    }
}
