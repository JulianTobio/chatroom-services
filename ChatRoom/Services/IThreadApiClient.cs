using ChatRoom.Models;

namespace ChatRoom.Services;

public interface IThreadApiClient
{
    Task<ThreadItem?> GetByIdAsync(
        int threadId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplyItem>> GetRepliesAsync(
        int threadId,
        CancellationToken cancellationToken = default);

    Task<ThreadItem> CreateAsync(
        CreateThreadRequest request,
        CancellationToken cancellationToken = default);

    Task<ReplyItem> CreateReplyAsync(
        int threadId,
        CreateReplyRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ThreadItem>> GetPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
