using System.ComponentModel.DataAnnotations;

namespace Thread.API.Models;

public class CreateThreadReplyRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
}
