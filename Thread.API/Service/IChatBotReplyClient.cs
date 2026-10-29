using Thread.API.Models;

namespace Thread.API.Service;

public interface IChatBotReplyClient
{
    Task<ReplyApiReply> CreateAsync(
        int threadId,
        string content,
        CancellationToken cancellationToken);
}
