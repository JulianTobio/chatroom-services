namespace Thread.API.Service;

public interface IChatBotClient
{
    Task<string> GenerateReplyAsync(string message, CancellationToken cancellationToken);
}
