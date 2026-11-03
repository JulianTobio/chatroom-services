using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;

public class RegisterRequest
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Lastname { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Mail { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Url, MaxLength(2048)]
    public string? Image { get; set; }
}
