using Users.API.Models;

namespace Users.API.Service;

public interface IUserProfileService
{
    Task<IReadOnlyList<PublicUserProfile>> GetByIdsAsync(IReadOnlyCollection<int> userIds);
}
