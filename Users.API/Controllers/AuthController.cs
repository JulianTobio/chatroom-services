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
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
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

        return Ok(jwtTokenService.CreateToken(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(jwtTokenService.CreateToken(user));
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
}
