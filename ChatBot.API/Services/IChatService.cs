using ChatBot.API.Models;

namespace ChatBot.API.Services;

public interface IChatService
{
    Task<ChatResponse> SendAsync(
        int userId,
        Guid conversationId,
        string message,
        CancellationToken cancellationToken);
}
