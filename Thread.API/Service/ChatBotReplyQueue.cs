using System.Threading.Channels;
using Thread.API.Models;

namespace Thread.API.Service;

public sealed class ChatBotReplyQueue : IChatBotReplyQueue
{
    private readonly Channel<ChatBotReplyJob> _channel =
        Channel.CreateBounded<ChatBotReplyJob>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryEnqueue(ChatBotReplyJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<ChatBotReplyJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
