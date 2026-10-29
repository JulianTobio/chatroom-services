using Thread.API.Models;

namespace Thread.API.Service;

public sealed class ChatBotReplyProcessor(
    IChatBotClient chatBotClient,
    IThreadService threadService,
    ILogger<ChatBotReplyProcessor> logger) : IChatBotReplyProcessor
{
    public async Task ProcessAsync(ChatBotReplyJob job, CancellationToken cancellationToken)
    {
        var response = await chatBotClient.GenerateReplyAsync(job.Prompt, cancellationToken);
        var reply = await threadService.CreateChatBotReplyAsync(
            job.ThreadId,
            response,
            cancellationToken);
        if (reply is null)
        {
            logger.LogWarning(
                "The chatbot reply for source reply {SourceReplyId} was not created because thread {ThreadId} no longer exists.",
                job.SourceReplyId,
                job.ThreadId);
            return;
        }

        logger.LogInformation(
            "Created chatbot reply {ReplyId} for source reply {SourceReplyId} in thread {ThreadId}.",
            reply.Id,
            job.SourceReplyId,
            job.ThreadId);
    }
}
