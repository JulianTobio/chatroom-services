namespace ChatRoom.Models;

public class PublicUserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Image { get; set; }
}
