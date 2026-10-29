using Thread.API.Models;

namespace Thread.API.Service;

public interface IChatBotReplyProcessor
{
    Task ProcessAsync(ChatBotReplyJob job, CancellationToken cancellationToken);
}
