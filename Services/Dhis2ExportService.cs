using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;

namespace Sanalink.API.Services;

public class Dhis2ExportService : IDhis2ExportService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<Dhis2ExportService> _logger;

    public Dhis2ExportService(
        AppDbContext db,
        HttpClient http,
        IConfiguration config,
        ILogger<Dhis2ExportService> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<Dhis2ExportResultDto> ExportAsync(string period, CancellationToken ct = default)
    {
        var baseUrl  = _config["DHIS2:BaseUrl"] ?? "";
        var username = _config["DHIS2:Username"] ?? "";
        var password = _config["DHIS2:Password"] ?? "";
        var dataSet  = _config["DHIS2:DataSet"] ?? "";

        if (string.IsNullOrWhiteSpace(baseUrl))
            return new Dhis2ExportResultDto { Success = false, Period = period, ErrorMessage = "DHIS2:BaseUrl is not configured." };

        // Parse period: "202601" → year=2026, month=1
        if (period.Length != 6 || !int.TryParse(period[..4], out int year) || !int.TryParse(period[4..], out int month))
            return new Dhis2ExportResultDto { Success = false, Period = period, ErrorMessage = "Period must be in yyyyMM format." };

        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd   = periodStart.AddMonths(1);

        // Build metric values
        var newPatients      = await _db.Patients.CountAsync(p => p.CreatedAt >= periodStart && p.CreatedAt < periodEnd, ct);
        var totalAppointments= await _db.Appointments.CountAsync(a => a.CreateAt >= periodStart && a.CreateAt < periodEnd, ct);
        var completedEncounters = await _db.Encounters.CountAsync(e => e.ClosedAt >= periodStart && e.ClosedAt < periodEnd, ct);
        var labOrders        = await _db.LabOrders.CountAsync(l => l.OrderedAt >= periodStart && l.OrderedAt < periodEnd, ct);

        var metricValues = new Dictionary<string, int>
        {
            ["NewPatientRegistrations"] = newPatients,
            ["TotalAppointments"]       = totalAppointments,
            ["CompletedEncounters"]     = completedEncounters,
            ["LabOrders"]               = labOrders,
        };

        // Load active mappings
        var mappings = await _db.Dhis2Mappings
            .Where(m => m.IsActive && m.OrgUnitUid != null)
            .ToListAsync(ct);

        if (!mappings.Any())
            return new Dhis2ExportResultDto { Success = false, Period = period, ErrorMessage = "No active DHIS2 mappings configured." };

        // Group by org unit for separate dataValueSet payloads
        var groups = mappings
            .Where(m => metricValues.ContainsKey(m.MetricName))
            .GroupBy(m => m.OrgUnitUid!);

        int totalExported = 0;

        foreach (var group in groups)
        {
            var payload = new
            {
                dataSet,
                completeDate = periodEnd.AddDays(-1).ToString("yyyy-MM-dd"),
                period,
                orgUnit = group.Key,
                dataValues = group.Select(m => new
                {
                    dataElement = m.DataElementUid,
                    value       = metricValues[m.MetricName].ToString()
                }).ToList()
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await _http.PostAsync($"{baseUrl.TrimEnd('/')}/api/dataValueSets", content, ct);

            if (response.IsSuccessStatusCode)
            {
                totalExported += group.Count();
                _logger.LogInformation("DHIS2 export: pushed {Count} values to org unit {OrgUnit} for period {Period}",
                    group.Count(), group.Key, period);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("DHIS2 export failed for org unit {OrgUnit}: {Status} — {Error}",
                    group.Key, response.StatusCode, error);
            }
        }

        return new Dhis2ExportResultDto
        {
            Success = totalExported > 0,
            Period = period,
            DataValuesExported = totalExported
        };
    }
}
