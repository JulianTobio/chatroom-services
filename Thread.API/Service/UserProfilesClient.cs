using System.Net.Http.Json;
using System.Text.Json;
using Thread.API.Models;

namespace Thread.API.Service;

public class UserProfilesClient(HttpClient httpClient) : IUserProfilesClient
{
    public async Task<IReadOnlyList<PublicUserProfile>> GetByIdsAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        using var response = await httpClient.PostAsJsonAsync(
            "api/users/profiles",
            new UserProfilesRequest { UserIds = ids });
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<List<PublicUserProfile>>()
                ?? throw new HttpRequestException("Users.API returned an empty profiles response.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Users.API returned an invalid profiles response.", exception);
        }
    }
}
