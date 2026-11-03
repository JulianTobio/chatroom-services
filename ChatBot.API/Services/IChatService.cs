using ChatBot.API.Models;

namespace ChatBot.API.Services;

public interface IChatService
{
    Task<ChatResponse> SendAsync(string message, CancellationToken cancellationToken);
}
