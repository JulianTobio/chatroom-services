using Reply.API.Models;

namespace Reply.API.Service
{
    public interface IReplyService
    {
        public Task<ReplyItem?> GetAsync(int replyId);

        public Task<PagedResult<ReplyItem>> GetByTopicAsync(int topicId, int pageNumber, int pageSize);

        public Task AddReplyAsync(ReplyItem item);

        public Task<bool> RemoveReplyAsync(int replyId);

        public Task<int> UpdateReplyAsync(int replyId, ReplyItem reply);
    }
}
