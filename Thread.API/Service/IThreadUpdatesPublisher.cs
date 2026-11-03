namespace Thread.API.Service;

public interface IThreadUpdatesPublisher
{
    Task PublishAsync(int threadId, string changeType);
}
