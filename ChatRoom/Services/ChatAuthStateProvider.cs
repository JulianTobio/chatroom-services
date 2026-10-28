using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class ChatAuthStateProvider(AuthApiClient authApiClient) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private ClaimsPrincipal _currentUser = Anonymous;
    private bool _initialized;

    public string? AccessToken { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public AuthUser? User { get; private set; }
    public string? RestoreError { get; private set; }

    public bool IsAuthenticated =>
        AccessToken is not null && ExpiresAtUtc > DateTime.UtcNow && _currentUser.Identity?.IsAuthenticated == true;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (AccessToken is not null
            && _currentUser.Identity?.IsAuthenticated == true
            && ExpiresAtUtc <= DateTime.UtcNow.AddMinutes(2))
        {
            await EnsureFreshAccessTokenAsync();
        }

        return new AuthenticationState(IsAuthenticated ? _currentUser : Anonymous);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            await RefreshSessionAsync();
        }
        catch (HttpRequestException exception)
        {
            RestoreError = exception.StatusCode is { } statusCode
                ? $"Users.API respondió HTTP {(int)statusCode} al comprobar la sesión guardada."
                : "No se pudo comprobar la sesión guardada. Puedes volver a intentarlo iniciando sesión.";
        }
        catch (TaskCanceledException)
        {
            RestoreError = "Users.API tardó demasiado en comprobar la sesión guardada.";
        }
    }

    public async Task<bool> EnsureFreshAccessTokenAsync()
    {
        if (AccessToken is null || _currentUser.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
        {
            return true;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (AccessToken is null || _currentUser.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            if (ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            {
                return true;
            }

            return await RefreshSessionAsync();
        }
        catch (HttpRequestException exception) when (exception.StatusCode is null)
        {
            return ExpiresAtUtc > DateTime.UtcNow;
        }
        catch (TaskCanceledException)
        {
            return ExpiresAtUtc > DateTime.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void SignIn(AuthResponse response)
    {
        AccessToken = response.Token;
        ExpiresAtUtc = response.ExpiresAtUtc;
        User = response.User;
        RestoreError = null;
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
        RestoreError = null;
        _currentUser = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private async Task<bool> RefreshSessionAsync()
    {
        var response = await authApiClient.RefreshAsync();
        if (response is null)
        {
            SignOut();
            return false;
        }

        SignIn(response);
        return true;
    }
}
