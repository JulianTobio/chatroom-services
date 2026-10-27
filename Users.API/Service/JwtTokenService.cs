using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Users.API.Models;

namespace Users.API.Service;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public AuthResponse CreateToken(ApplicationUser user)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configure Jwt:Issuer.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Configure Jwt:Audience.");
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Configure Jwt:SigningKey using an environment variable or user secrets.");
        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("name", user.Name),
            new Claim("lastname", user.Lastname)
        };
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new UserResponse(
                user.Id,
                user.UserName!,
                user.Name,
                user.Lastname,
                user.Email!,
                user.Image));
    }
}
