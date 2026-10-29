namespace ChatBot.API.Models;

public sealed class ChatBotOptions
{
    public const string SectionName = "ChatBot";

    public string BaseAddress { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
}
