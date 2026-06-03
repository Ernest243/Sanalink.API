namespace Sanalink.API.Services;

public interface IDataRetentionService
{
    Task PurgeOldAuditLogsAsync(int retentionDays);
    Task PurgeExpiredTokensAsync();
}
