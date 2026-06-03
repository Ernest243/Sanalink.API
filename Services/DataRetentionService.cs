using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;

namespace Sanalink.API.Services;

public class DataRetentionService : IDataRetentionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(AppDbContext db, ILogger<DataRetentionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PurgeOldAuditLogsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = await _db.AuditLogs
            .Where(a => a.Timestamp < cutoff)
            .ExecuteDeleteAsync();
        _logger.LogInformation("Data retention: purged {Count} audit logs older than {Days} days", deleted, retentionDays);
    }

    public async Task PurgeExpiredTokensAsync()
    {
        var deleted = await _db.TokenBlacklists
            .Where(t => t.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
        _logger.LogInformation("Data retention: purged {Count} expired token blacklist entries", deleted);
    }
}
