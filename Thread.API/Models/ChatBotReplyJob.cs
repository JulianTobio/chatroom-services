namespace Thread.API.Models;

public sealed record ChatBotReplyJob(int ThreadId, int SourceReplyId, string Prompt);
