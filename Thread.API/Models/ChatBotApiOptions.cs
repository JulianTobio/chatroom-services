namespace Thread.API.Models;

public sealed class ChatBotApiOptions
{
    public const string SectionName = "ChatBotApi";

    public string BaseAddress { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
}
