using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users.API.Models;
using Users.API.Service;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    private string RefreshCookieName =>
        configuration["RefreshTokens:CookieName"] ?? "chatroom.refresh";

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!string.IsNullOrWhiteSpace(request.Image)
            && (!Uri.TryCreate(request.Image, UriKind.Absolute, out var imageUri)
                || imageUri.Scheme is not ("http" or "https")))
        {
            ModelState.AddModelError(nameof(request.Image), "La imagen debe ser una URL HTTP o HTTPS.");
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(),
            Email = request.Mail.Trim(),
            Name = request.Name.Trim(),
            Lastname = request.Lastname.Trim(),
            Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim(),
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        var refreshSession = await refreshTokenService.CreateSessionAsync(user);
        SetRefreshCookie(refreshSession.RotatedToken!, refreshSession.ExpiresAtUtc);
        return Ok(jwtTokenService.CreateToken(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var user = await userManager.FindByNameAsync(request.Username.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var refreshSession = await refreshTokenService.CreateSessionAsync(user);
        SetRefreshCookie(refreshSession.RotatedToken!, refreshSession.ExpiresAtUtc);
        return Ok(jwtTokenService.CreateToken(user));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        var refreshSession = await refreshTokenService.RefreshAsync(token);
        if (refreshSession is null)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        if (refreshSession.RotatedToken is not null)
        {
            SetRefreshCookie(refreshSession.RotatedToken, refreshSession.ExpiresAtUtc);
        }

        return Ok(jwtTokenService.CreateToken(refreshSession.User));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (Request.Cookies.TryGetValue(RefreshCookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            await refreshTokenService.RevokeAsync(token);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null
            ? NotFound()
            : Ok(new UserResponse(
                user.Id,
                user.UserName!,
                user.Name,
                user.Lastname,
                user.Email!,
                user.Image));
    }

    private bool HasTrustedOrigin()
    {
        if (!Request.Headers.TryGetValue("Origin", out var requestOrigin))
        {
            return false;
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? [];
        return allowedOrigins.Contains(requestOrigin.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        var now = DateTime.UtcNow;
        Response.Cookies.Append(RefreshCookieName, token, CreateCookieOptions(expiresAtUtc, now));
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, CreateCookieOptions(DateTime.UnixEpoch, DateTime.UtcNow));
    }

    private CookieOptions CreateCookieOptions(DateTime expiresAtUtc, DateTime now) =>
        new()
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            MaxAge = expiresAtUtc > now ? expiresAtUtc - now : TimeSpan.Zero
        };
}
