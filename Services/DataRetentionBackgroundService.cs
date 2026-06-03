namespace Sanalink.API.Services;

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    public DataRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run at 02:00 UTC every night
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2);
            await Task.Delay(nextRun - now, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var retentionDays = _config.GetValue<int>("Compliance:DataRetentionDays", 365);
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();
                await svc.PurgeOldAuditLogsAsync(retentionDays);
                await svc.PurgeExpiredTokensAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data retention job failed");
            }
        }
    }
}
