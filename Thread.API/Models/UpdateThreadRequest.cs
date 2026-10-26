using System.ComponentModel.DataAnnotations;

namespace Thread.API.Models;

public class UpdateThreadRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int Author { get; set; }
}
