namespace Sanalink.API.Services;

/// <summary>Pushes aggregate data to DHIS2 on the 1st of each month for the previous month.</summary>
public class Dhis2ExportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Dhis2ExportBackgroundService> _logger;

    public Dhis2ExportBackgroundService(IServiceScopeFactory scopeFactory, ILogger<Dhis2ExportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Next run: 1st of next month at 03:00 UTC
            var nextRun = new DateTime(now.Year, now.Month, 1, 3, 0, 0, DateTimeKind.Utc).AddMonths(1);
            await Task.Delay(nextRun - now, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            // Export the previous calendar month
            var previousMonth = now.AddMonths(-1);
            var period = previousMonth.ToString("yyyyMM");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IDhis2ExportService>();
                var result = await svc.ExportAsync(period, stoppingToken);

                if (result.Success)
                    _logger.LogInformation("DHIS2 monthly export succeeded: {Count} values for {Period}", result.DataValuesExported, period);
                else
                    _logger.LogWarning("DHIS2 monthly export had issues for {Period}: {Error}", period, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DHIS2 monthly export threw for period {Period}", period);
            }
        }
    }
}
