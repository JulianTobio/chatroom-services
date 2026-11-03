using Microsoft.AspNetCore.Identity;

namespace Users.API.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string? Image { get; set; }
}
