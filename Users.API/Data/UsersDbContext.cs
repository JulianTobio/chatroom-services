using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Users.API.Models;

namespace Users.API.Data;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<int>, int>(options)
{
}
