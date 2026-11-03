using Thread.API.Models;

namespace Thread.API.Service;

public interface IChatBotReplyQueue
{
    bool TryEnqueue(ChatBotReplyJob job);

    IAsyncEnumerable<ChatBotReplyJob> ReadAllAsync(CancellationToken cancellationToken);
}
