namespace ChatBot.API.Models;

public sealed record ChatResponse(Guid ConversationId, string Response);
