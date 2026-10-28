using Users.API.Models;

namespace Users.API.Service;

public interface IRefreshTokenService
{
    Task<RefreshSessionResult> CreateSessionAsync(ApplicationUser user);

    Task<RefreshSessionResult?> RefreshAsync(string token);

    Task RevokeAsync(string token);
}

public record RefreshSessionResult(
    ApplicationUser User,
    string? RotatedToken,
    DateTime ExpiresAtUtc);
