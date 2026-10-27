namespace ChatRoom.Models;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Image { get; set; }
}
