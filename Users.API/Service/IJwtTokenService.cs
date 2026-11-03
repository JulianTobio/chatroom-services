using Users.API.Models;

namespace Users.API.Service;

public interface IJwtTokenService
{
    AuthResponse CreateToken(ApplicationUser user);
}
