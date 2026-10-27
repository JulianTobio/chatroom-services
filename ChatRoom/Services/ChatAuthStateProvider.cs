using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class ChatAuthStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser = Anonymous;

    public string? AccessToken { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public AuthUser? User { get; private set; }

    public bool IsAuthenticated =>
        AccessToken is not null && ExpiresAtUtc > DateTime.UtcNow && _currentUser.Identity?.IsAuthenticated == true;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (AccessToken is not null && ExpiresAtUtc <= DateTime.UtcNow)
        {
            SignOut();
        }

        return Task.FromResult(new AuthenticationState(IsAuthenticated ? _currentUser : Anonymous));
    }

    public void SignIn(AuthResponse response)
    {
        AccessToken = response.Token;
        ExpiresAtUtc = response.ExpiresAtUtc;
        User = response.User;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
            new Claim(ClaimTypes.Name, response.User.Username),
            new Claim(ClaimTypes.Email, response.User.Mail)
        ], authenticationType: "jwt"));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        AccessToken = null;
        ExpiresAtUtc = DateTime.MinValue;
        User = null;
        _currentUser = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}
