using ChatBot.API.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ChatApiResponse = ChatBot.API.Models.ChatResponse;

namespace ChatBot.API.Services;

public sealed class ChatService(
    IChatClient chatClient,
    IOptions<ChatBotOptions> options) : IChatService
{
    public async Task<ChatApiResponse> SendAsync(string message, CancellationToken cancellationToken)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, options.Value.SystemPrompt),
            new ChatMessage(ChatRole.User, message)
        };
        var result = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return new ChatApiResponse(result.Text);
    }
}
