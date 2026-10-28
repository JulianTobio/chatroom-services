using Reply.API.Models;

namespace Reply.API.Service;

public interface IUserProfilesClient
{
    Task<IReadOnlyList<PublicUserProfile>> GetByIdsAsync(IEnumerable<int> userIds);
}
