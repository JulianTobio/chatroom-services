using Thread.API.Models;

namespace Thread.API.Service;

public interface IReplyApiClient
{
    Task<ReplyApiReply> CreateAsync(int threadId, CreateThreadReplyRequest request);

    Task<ReplyApiReply?> GetAsync(int replyId);

    Task<bool> DeleteAsync(int replyId);

    Task<PagedReplyApiResponse> GetByThreadAsync(int threadId, int pageNumber, int pageSize);
}
