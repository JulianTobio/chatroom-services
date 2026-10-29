using System.Net.Http.Json;
using System.Text.Json;
using Thread.API.Models;

namespace Thread.API.Service;

public sealed class ChatBotReplyClient(HttpClient httpClient) : IChatBotReplyClient
{
    public async Task<ReplyApiReply> CreateAsync(
        int threadId,
        string content,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/internal/chatbot/replies",
            new ChatBotReplyCreateRequest(threadId, content),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<ReplyApiReply>(cancellationToken)
                ?? throw new HttpRequestException("Reply.API returned an empty chatbot reply.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Reply.API returned an invalid chatbot reply.", exception);
        }
    }
}
