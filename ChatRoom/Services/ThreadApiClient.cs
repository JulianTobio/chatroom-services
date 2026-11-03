using System.Net.Http.Json;
using System.Text.Json;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class ThreadApiClient(HttpClient httpClient) : IThreadApiClient
{
    public async Task<ThreadItem?> GetByIdAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/threads/{threadId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ThreadItem>(
                   cancellationToken: cancellationToken)
               ?? throw new System.Text.Json.JsonException("Thread.API returned an empty thread response.");
    }

    public async Task<IReadOnlyList<ReplyItem>> GetRepliesAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/threads/{threadId}/replies",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<ReplyItem>>(
                   cancellationToken: cancellationToken)
               ?? throw new System.Text.Json.JsonException("Thread.API returned an empty replies response.");
    }

    public async Task<ThreadItem> CreateAsync(
        CreateThreadRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/threads",
            request,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<ThreadItem>(
                   cancellationToken: cancellationToken)
               ?? throw new System.Text.Json.JsonException("Thread.API returned an empty thread response.");
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemResponse>(
                cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }

            if (problem?.Errors is not null)
            {
                var validationErrors = problem.Errors
                    .SelectMany(field => field.Value.Select(message => $"{field.Key}: {message}"));
                var message = string.Join(" ", validationErrors);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch (JsonException)
        {
            // Fall back to the HTTP reason phrase when the API does not return a problem-details body.
        }

        return response.ReasonPhrase ?? "La API devolvió una respuesta de error.";
    }

    private sealed class ApiProblemResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    public async Task<ReplyItem> CreateReplyAsync(
        int threadId,
        CreateReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/threads/{threadId}/replies",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ReplyItem>(
                   cancellationToken: cancellationToken)
               ?? throw new System.Text.Json.JsonException("Thread.API returned an empty reply response.");
    }

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
