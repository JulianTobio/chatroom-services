using System.Net.Http.Json;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class ThreadApiClient(HttpClient httpClient) : IThreadApiClient
{
    public async Task<PagedResult<ThreadItem>> GetPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/threads?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResult<ThreadItem>>(
                   cancellationToken: cancellationToken)
               ?? throw new System.Text.Json.JsonException("Thread.API returned an empty paginated response.");
    }
}
