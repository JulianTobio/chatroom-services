using ThreadModel = Thread.API.Models.Thread;
using Thread.API.Models;

namespace Thread.API.Service;

public interface IThreadService
{
    Task<PagedResult<ThreadModel>> GetAllAsync(int pageNumber, int pageSize);

    Task<ThreadModel?> GetAsync(int threadId);

    Task<ThreadModel> AddAsync(ThreadModel thread);

    Task<bool> UpdateAsync(int threadId, ThreadModel thread);

    Task<bool> RemoveAsync(int threadId);

    Task<ReplyApiReply?> CreateReplyAsync(int threadId, CreateThreadReplyRequest request);

    Task<bool> DeleteReplyAsync(int threadId, int replyId, int userId);

    Task<IReadOnlyList<ReplyApiReply>?> GetRepliesAsync(int threadId);
}
