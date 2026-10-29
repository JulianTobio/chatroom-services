using System.ComponentModel.DataAnnotations;

namespace ChatBot.API.Models;

public sealed record ChatRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}
