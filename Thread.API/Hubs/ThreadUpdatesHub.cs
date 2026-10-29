using Microsoft.AspNetCore.SignalR;

namespace Thread.API.Hubs;

public sealed class ThreadUpdatesHub : Hub
{
    public const string ThreadChangedEvent = "ThreadChanged";
    public const string ThreadListGroup = "thread-list";

    public Task SubscribeToThreadList() =>
        Groups.AddToGroupAsync(Context.ConnectionId, ThreadListGroup);

    public Task UnsubscribeFromThreadList() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ThreadListGroup);

    public Task SubscribeToThread(int threadId)
    {
        if (threadId <= 0)
        {
            throw new HubException("Thread ID must be positive.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GetThreadGroup(threadId));
    }

    public Task UnsubscribeFromThread(int threadId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetThreadGroup(threadId));

    public static string GetThreadGroup(int threadId) => $"thread-{threadId}";
}
