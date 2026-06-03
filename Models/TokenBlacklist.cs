using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Models;

/// <summary>Revoked JWT entries — checked by TokenRevocationMiddleware when ISO27001_TokenRevocation is on.</summary>
public class TokenBlacklist
{
    public int Id { get; set; }

    /// <summary>JWT ID claim (jti) of the revoked token.</summary>
    [Required]
    [MaxLength(100)]
    public string Jti { get; set; } = default!;

    [MaxLength(450)]
    public string? UserId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
