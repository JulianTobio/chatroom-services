using System.ComponentModel.DataAnnotations;

namespace Reply.API.Models;

public sealed class CreateChatBotReplyRequest
{
    [Required, StringLength(16000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TopicId { get; set; }
}
