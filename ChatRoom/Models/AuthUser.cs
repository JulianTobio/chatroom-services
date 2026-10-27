namespace ChatRoom.Models;

public record AuthUser(
    int Id,
    string Username,
    string Name,
    string Lastname,
    string Mail,
    string? Image);
