using Microsoft.Extensions.DependencyInjection;

namespace Thread.API.Service;

public sealed class ChatBotReplyWorker(
    IChatBotReplyQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatBotReplyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IChatBotReplyProcessor>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process chatbot mention from reply {SourceReplyId} in thread {ThreadId}.",
                    job.SourceReplyId,
                    job.ThreadId);
            }
        }
    }
}
