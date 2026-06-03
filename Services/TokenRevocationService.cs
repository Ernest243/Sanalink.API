using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.Models;

namespace Sanalink.API.Services;

public class TokenRevocationService : ITokenRevocationService
{
    private readonly AppDbContext _db;

    public TokenRevocationService(AppDbContext db) => _db = db;

    public async Task RevokeAsync(string jti, string userId, DateTime expiresAt)
    {
        _db.TokenBlacklists.Add(new TokenBlacklist
        {
            Jti       = jti,
            UserId    = userId,
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public Task<bool> IsRevokedAsync(string jti)
        => _db.TokenBlacklists.AnyAsync(t => t.Jti == jti && t.ExpiresAt > DateTime.UtcNow);

    public async Task PurgeExpiredAsync()
    {
        await _db.TokenBlacklists
            .Where(t => t.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }
}
