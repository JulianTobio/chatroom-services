using Reply.API.Models;

namespace Reply.API.Service
{
    public interface IReplyService
    {
        public Task<ReplyItem?> GetAsync(int replyId);

        public Task<ReplyResponse?> GetResponseAsync(int replyId);

        public Task<PagedResult<ReplyResponse>> GetByTopicAsync(int topicId, int pageNumber, int pageSize);

        public Task<ReplyResponse> AddReplyAsync(ReplyItem item);

        public Task<ReplyResponse> AddChatBotReplyAsync(ReplyItem item);

        public Task<bool> RemoveReplyAsync(int replyId, int userId);

        public Task<int> UpdateReplyAsync(int replyId, int userId, string content);
    }
}
