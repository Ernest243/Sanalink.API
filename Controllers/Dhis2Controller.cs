using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Sanalink.API.Infrastructure;
using Sanalink.API.Services;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/v1/dhis2")]
[Authorize(Roles = "Admin,DAF")]
[FeatureGate(FeatureFlags.DHIS2)]
public class Dhis2Controller : ControllerBase
{
    private readonly IDhis2ExportService _exportService;

    public Dhis2Controller(IDhis2ExportService exportService) => _exportService = exportService;

    /// <summary>
    /// Manually trigger a DHIS2 export for a given period.
    /// Period format: yyyyMM (e.g. "202601" for January 2026).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromQuery] string period)
    {
        if (string.IsNullOrWhiteSpace(period) || period.Length != 6)
            return BadRequest("Period must be in yyyyMM format, e.g. '202601'.");

        var result = await _exportService.ExportAsync(period, HttpContext.RequestAborted);
        return result.Success ? Ok(result) : StatusCode(502, result);
    }
}
