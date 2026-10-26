using ChatRoom.Models;

namespace ChatRoom.Services;

public interface IThreadApiClient
{
    Task<PagedResult<ThreadItem>> GetPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
