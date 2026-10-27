namespace Users.API.Models;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UserResponse User);
