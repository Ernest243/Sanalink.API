namespace Sanalink.API.Services;

public class Dhis2ExportResultDto
{
    public bool Success { get; set; }
    public string Period { get; set; } = default!;
    public int DataValuesExported { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IDhis2ExportService
{
    Task<Dhis2ExportResultDto> ExportAsync(string period, CancellationToken ct = default);
}
