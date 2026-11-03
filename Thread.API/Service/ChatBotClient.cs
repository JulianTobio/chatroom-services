using System.Net.Http.Json;
using System.Text.Json;
using Thread.API.Models;

namespace Thread.API.Service;

public sealed class ChatBotClient(HttpClient httpClient) : IChatBotClient
{
    public async Task<string> GenerateReplyAsync(
        string message,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/chat/internal",
            new ChatBotApiRequest(message),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        try
        {
            var result = await response.Content.ReadFromJsonAsync<ChatBotApiResponse>(
                cancellationToken);
            return string.IsNullOrWhiteSpace(result?.Response)
                ? throw new HttpRequestException("ChatBot.API returned an empty response.")
                : result.Response.Trim();
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("ChatBot.API returned an invalid response.", exception);
        }
    }
}
