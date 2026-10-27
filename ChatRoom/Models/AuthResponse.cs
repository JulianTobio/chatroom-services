namespace ChatRoom.Models;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    AuthUser User);
