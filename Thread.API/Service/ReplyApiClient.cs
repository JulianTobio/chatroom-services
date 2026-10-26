using System.Net.Http.Json;
using System.Text.Json;
using Thread.API.Models;

namespace Thread.API.Service;

public class ReplyApiClient(HttpClient httpClient) : IReplyApiClient
{
    public async Task<ReplyApiReply> CreateAsync(int threadId, CreateThreadReplyRequest request)
    {
        var replyRequest = new ReplyApiCreateRequest
        {
            Content = request.Content,
            TopicId = threadId,
            UserId = request.UserId
        };

        using var response = await httpClient.PostAsJsonAsync("api/replies", replyRequest);
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<ReplyApiReply>()
                ?? throw new HttpRequestException("Reply.API returned an empty reply response.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Reply.API returned an invalid reply response.", exception);
        }
    }

    public async Task<bool> DeleteAsync(int replyId)
    {
        using var response = await httpClient.DeleteAsync($"api/replies/{replyId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<ReplyApiReply?> GetAsync(int replyId)
    {
        using var response = await httpClient.GetAsync($"api/replies/{replyId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        try
        {
            return await response.Content.ReadFromJsonAsync<ReplyApiReply>()
                ?? throw new HttpRequestException("Reply.API returned an empty reply response.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Reply.API returned an invalid reply response.", exception);
        }
    }

    public async Task<PagedReplyApiResponse> GetByThreadAsync(int threadId, int pageNumber, int pageSize)
    {
        using var response = await httpClient.GetAsync(
            $"api/replies/topic/{threadId}?pageNumber={pageNumber}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<PagedReplyApiResponse>()
                ?? throw new HttpRequestException("Reply.API returned an empty paginated reply response.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Reply.API returned an invalid paginated reply response.", exception);
        }
    }

}
