using System.ComponentModel.DataAnnotations;

namespace Reply.API.Models;

public class UserProfilesRequest
{
    [Required, MaxLength(100)]
    public int[] UserIds { get; set; } = [];
}
