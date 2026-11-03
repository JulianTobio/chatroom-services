using Thread.API.Models;

namespace Thread.API.Service;

public interface IUserProfilesClient
{
    Task<IReadOnlyList<PublicUserProfile>> GetByIdsAsync(IEnumerable<int> userIds);
}
