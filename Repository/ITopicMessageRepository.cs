using FinChain.Model.PostgreSQL;

namespace FinChain.Repository
{
    public interface ITopicMessageRepository : ISupabaseRepository<topic_message>
    {
        Task<List<topic_message>> GetActiveTopicsAsync();
    }
}
