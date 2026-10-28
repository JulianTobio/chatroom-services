using ChatBot.API.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ChatApiResponse = ChatBot.API.Models.ChatResponse;

namespace ChatBot.API.Services;

public sealed class ChatService(
    IChatClient chatClient,
    IMemoryCache memoryCache,
    IOptions<ChatBotOptions> options) : IChatService
{
    private readonly SemaphoreSlim _cacheGate = new(1, 1);

    public async Task<ChatApiResponse> SendAsync(
        int userId,
        Guid conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var cacheKey = (userId, conversationId);
        await _cacheGate.WaitAsync(cancellationToken);
        ConversationState conversation;
        try
        {
            conversation = memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(settings.ConversationIdleTimeoutMinutes);
                return new ConversationState(settings.SystemPrompt);
            }) ?? throw new InvalidOperationException("The conversation could not be initialized.");
        }
        finally
        {
            _cacheGate.Release();
        }

        await conversation.Gate.WaitAsync(cancellationToken);
        try
        {
            var messages = conversation.Messages.ToList();
            messages.Add(new ChatMessage(ChatRole.User, message));
            var result = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var response = result.Text;

            conversation.Messages.Add(new ChatMessage(ChatRole.User, message));
            conversation.Messages.Add(new ChatMessage(ChatRole.Assistant, response));
            TrimHistory(conversation.Messages, settings.MaxHistoryTurns);

            return new ChatApiResponse(conversationId, response);
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    private static void TrimHistory(List<ChatMessage> messages, int maxHistoryTurns)
    {
        while (messages.Count > 1 + maxHistoryTurns * 2)
        {
            messages.RemoveRange(1, 2);
        }
    }

    private sealed class ConversationState
    {
        public ConversationState(string systemPrompt)
        {
            Messages = [new ChatMessage(ChatRole.System, systemPrompt)];
        }

        public SemaphoreSlim Gate { get; } = new(1, 1);
        public List<ChatMessage> Messages { get; }
    }
}
