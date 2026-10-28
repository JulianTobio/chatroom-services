using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public RefreshSession Session { get; set; } = null!;

    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
