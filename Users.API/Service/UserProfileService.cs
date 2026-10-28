using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Users.API.Models;

namespace Users.API.Service;

public class UserProfileService(UserManager<ApplicationUser> userManager) : IUserProfileService
{
    public async Task<IReadOnlyList<PublicUserProfile>> GetByIdsAsync(IReadOnlyCollection<int> userIds)
    {
        var requestedIds = userIds.Distinct().ToArray();
        if (requestedIds.Length == 0)
        {
            return [];
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => requestedIds.Contains(user.Id))
            .OrderBy(user => user.Id)
            .Select(user => new PublicUserProfile(user.Id, user.UserName!, user.Image))
            .ToListAsync();
    }
}
