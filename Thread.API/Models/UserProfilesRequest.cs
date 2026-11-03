using System.ComponentModel.DataAnnotations;

namespace Thread.API.Models;

public class UserProfilesRequest
{
    [Required, MaxLength(100)]
    public int[] UserIds { get; set; } = [];
}
