using Microsoft.AspNetCore.SignalR.Client;

namespace ChatRoom.Services;

public sealed class ThreadUpdatesClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<ThreadUpdatesClient> _logger;
    private readonly SemaphoreSlim _subscriptionLock = new(1, 1);
    private readonly HashSet<int> _subscribedThreads = [];
    private bool _subscribedToThreadList;

    public ThreadUpdatesClient(
        Uri threadApiAddress,
        ILogger<ThreadUpdatesClient> logger)
    {
        _logger = logger;
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(threadApiAddress, "hubs/threads"))
            .WithAutomaticReconnect()
            .Build();
        _connection.On<int, string>(
            "ThreadChanged",
            HandleThreadChangedAsync);
        _connection.Reconnected += HandleReconnectedAsync;
    }

    public event Func<int, string, Task>? ThreadChanged;
    public event Func<Task>? Reconnected;

    public async Task SubscribeToThreadListAsync()
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            await EnsureConnectedAsync();
            if (_subscribedToThreadList)
            {
                return;
            }

            await _connection.InvokeAsync("SubscribeToThreadList");
            _subscribedToThreadList = true;
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async Task UnsubscribeFromThreadListAsync()
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            if (!_subscribedToThreadList)
            {
                return;
            }

            _subscribedToThreadList = false;
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("UnsubscribeFromThreadList");
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not leave the thread list SignalR group.");
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async Task SubscribeToThreadAsync(int threadId)
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            await EnsureConnectedAsync();
            if (_subscribedThreads.Contains(threadId))
            {
                return;
            }

            await _connection.InvokeAsync("SubscribeToThread", threadId);
            _subscribedThreads.Add(threadId);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async Task UnsubscribeFromThreadAsync(int threadId)
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            if (!_subscribedThreads.Remove(threadId))
            {
                return;
            }

            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("UnsubscribeFromThread", threadId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not leave the SignalR group for thread {ThreadId}.",
                threadId);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _subscriptionLock.Dispose();
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }

        if (_connection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException(
                $"The SignalR connection is currently {_connection.State}.");
        }
    }

    private async Task HandleThreadChangedAsync(int threadId, string changeType)
    {
        var handlers = ThreadChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<int, string, Task>>())
        {
            try
            {
                await handler(threadId, changeType);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "A UI handler failed while processing {ChangeType} for thread {ThreadId}.",
                    changeType,
                    threadId);
            }
        }
    }

    private async Task HandleReconnectedAsync(string? connectionId)
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            if (_subscribedToThreadList)
            {
                await _connection.InvokeAsync("SubscribeToThreadList");
            }

            foreach (var threadId in _subscribedThreads)
            {
                await _connection.InvokeAsync("SubscribeToThread", threadId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not restore SignalR subscriptions after reconnecting.");
            return;
        }
        finally
        {
            _subscriptionLock.Release();
        }

        var handlers = Reconnected;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A UI handler failed after the SignalR connection recovered.");
            }
        }
    }
}
