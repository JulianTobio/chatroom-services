using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Users.API.Data;
using Users.API.Models;

namespace Users.API.Service;

public class RefreshTokenService(
    UsersDbContext context,
    IConfiguration configuration,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private readonly int _slidingExpirationDays =
        configuration.GetValue<int?>("RefreshTokens:SlidingExpirationDays") ?? 30;
    private readonly int _absoluteExpirationDays =
        configuration.GetValue<int?>("RefreshTokens:AbsoluteExpirationDays") ?? 90;
    private readonly int _concurrencyGraceSeconds =
        configuration.GetValue<int?>("RefreshTokens:ConcurrencyGraceSeconds") ?? 15;

    public async Task<RefreshSessionResult> CreateSessionAsync(ApplicationUser user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await context.RefreshSessions
            .Where(session => session.AbsoluteExpiresAtUtc <= now
                || (session.RevokedAtUtc != null && session.RevokedAtUtc <= now.AddDays(-30)))
            .ExecuteDeleteAsync();

        var absoluteExpiration = now.AddDays(_absoluteExpirationDays);
        var expiration = now.AddDays(_slidingExpirationDays);
        var session = new RefreshSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAtUtc = now,
            LastRefreshedAtUtc = now,
            AbsoluteExpiresAtUtc = absoluteExpiration,
            ExpiresAtUtc = expiration
        };
        var token = GenerateToken();
        context.RefreshSessions.Add(session);
        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = HashToken(token),
            IssuedAtUtc = now,
            ExpiresAtUtc = expiration
        });

        await context.SaveChangesAsync();
        return new RefreshSessionResult(user, token, expiration);
    }

    public async Task<RefreshSessionResult?> RefreshAsync(string token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = HashToken(token);
        var storedToken = await FindTokenAsync(tokenHash);
        if (storedToken is null)
        {
            return null;
        }

        var session = storedToken.Session;
        if (session.RevokedAtUtc is not null
            || session.ExpiresAtUtc <= now
            || session.AbsoluteExpiresAtUtc <= now
            || storedToken.ExpiresAtUtc <= now)
        {
            await RevokeSessionAsync(session, now);
            return null;
        }

        if (storedToken.ConsumedAtUtc is { } consumedAt)
        {
            if (consumedAt.AddSeconds(_concurrencyGraceSeconds) >= now)
            {
                return new RefreshSessionResult(session.User, null, session.ExpiresAtUtc);
            }

            await RevokeSessionAsync(session, now);
            return null;
        }

        var refreshedToken = GenerateToken();
        storedToken.ConsumedAtUtc = now;
        session.LastRefreshedAtUtc = now;
        session.ExpiresAtUtc = Min(now.AddDays(_slidingExpirationDays), session.AbsoluteExpiresAtUtc);
        await context.RefreshTokens
            .Where(refreshToken => refreshToken.SessionId == session.Id
                && refreshToken.ConsumedAtUtc != null
                && refreshToken.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();
        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = HashToken(refreshedToken),
            IssuedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc
        });

        try
        {
            await context.SaveChangesAsync();
            return new RefreshSessionResult(session.User, refreshedToken, session.ExpiresAtUtc);
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            var concurrentlyRefreshedToken = await FindTokenAsync(tokenHash);
            if (concurrentlyRefreshedToken is null
                || concurrentlyRefreshedToken.Session.RevokedAtUtc is not null)
            {
                return null;
            }

            if (concurrentlyRefreshedToken.ConsumedAtUtc is { } concurrentConsumedAt)
            {
                if (concurrentConsumedAt.AddSeconds(_concurrencyGraceSeconds)
                    >= timeProvider.GetUtcNow().UtcDateTime)
                {
                    return new RefreshSessionResult(
                        concurrentlyRefreshedToken.Session.User,
                        null,
                        concurrentlyRefreshedToken.Session.ExpiresAtUtc);
                }

                await RevokeSessionAsync(
                    concurrentlyRefreshedToken.Session,
                    timeProvider.GetUtcNow().UtcDateTime);
                return null;
            }

            throw;
        }
    }

    public async Task RevokeAsync(string token)
    {
        var storedToken = await FindTokenAsync(HashToken(token));
        if (storedToken is null || storedToken.Session.RevokedAtUtc is not null)
        {
            return;
        }

        await RevokeSessionAsync(storedToken.Session, timeProvider.GetUtcNow().UtcDateTime);
    }

    private Task<RefreshToken?> FindTokenAsync(string tokenHash) =>
        context.RefreshTokens
            .Include(refreshToken => refreshToken.Session)
            .ThenInclude(session => session.User)
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash);

    private async Task RevokeSessionAsync(RefreshSession session, DateTime now)
    {
        session.RevokedAtUtc = now;
        await context.SaveChangesAsync();
    }

    private static string GenerateToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static DateTime Min(DateTime first, DateTime second) =>
        first <= second ? first : second;
}
