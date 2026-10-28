using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;

public record PublicUserProfile(int Id, string Username, string? Image);

public class UserProfilesRequest
{
    [Required, MaxLength(100)]
    public List<int> UserIds { get; set; } = [];
}
