using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Thread.API.Models;

namespace Thread.API.Service;

public class ReplyApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IReplyApiClient
{
    public async Task<ReplyApiReply> CreateAsync(ReplyApiCreateRequest replyRequest)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/replies")
        {
            Content = JsonContent.Create(replyRequest)
        };
        ForwardAccessToken(requestMessage);
        using var response = await httpClient.SendAsync(requestMessage);
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
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"api/replies/{replyId}");
        ForwardAccessToken(requestMessage);
        using var response = await httpClient.SendAsync(requestMessage);
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

    private void ForwardAccessToken(HttpRequestMessage request)
    {
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var authorizationHeader))
        {
            request.Headers.Authorization = authorizationHeader;
        }
    }
}
