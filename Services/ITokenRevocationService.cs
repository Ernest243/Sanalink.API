namespace Sanalink.API.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, string userId, DateTime expiresAt);
    Task<bool> IsRevokedAsync(string jti);
    Task PurgeExpiredAsync();
}
