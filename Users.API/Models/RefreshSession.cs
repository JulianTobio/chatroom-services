using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;

public class RefreshSession
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime AbsoluteExpiresAtUtc { get; set; }
    public DateTime LastRefreshedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<RefreshToken> Tokens { get; set; } = [];
}
