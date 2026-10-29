using Microsoft.AspNetCore.SignalR;
using Thread.API.Hubs;

namespace Thread.API.Service;

public sealed class ThreadUpdatesPublisher(
    IHubContext<ThreadUpdatesHub> hubContext,
    ILogger<ThreadUpdatesPublisher> logger) : IThreadUpdatesPublisher
{
    public async Task PublishAsync(int threadId, string changeType)
    {
        await SendToGroupAsync(ThreadUpdatesHub.ThreadListGroup, threadId, changeType);
        await SendToGroupAsync(ThreadUpdatesHub.GetThreadGroup(threadId), threadId, changeType);
    }

    private async Task SendToGroupAsync(string groupName, int threadId, string changeType)
    {
        try
        {
            await hubContext.Clients
                .Group(groupName)
                .SendAsync(ThreadUpdatesHub.ThreadChangedEvent, threadId, changeType);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not publish the {ChangeType} notification for thread {ThreadId} to group {GroupName}.",
                changeType,
                threadId,
                groupName);
        }
    }
}
