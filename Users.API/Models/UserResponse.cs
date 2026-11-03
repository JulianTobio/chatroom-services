namespace Users.API.Models;

public record UserResponse(
    int Id,
    string Username,
    string Name,
    string Lastname,
    string Mail,
    string? Image);
