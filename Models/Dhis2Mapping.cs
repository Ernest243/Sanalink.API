using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Models;

/// <summary>Maps a local aggregate metric to a DHIS2 Data Element and Organisation Unit.</summary>
public class Dhis2Mapping
{
    public int Id { get; set; }

    /// <summary>Internal metric name, e.g. "NewPatientRegistrations", "TotalAppointments".</summary>
    [Required]
    [MaxLength(100)]
    public string MetricName { get; set; } = default!;

    /// <summary>DHIS2 Data Element UID (11-char alphanumeric).</summary>
    [Required]
    [MaxLength(11)]
    public string DataElementUid { get; set; } = default!;

    /// <summary>Scope the metric to a specific facility (null = all facilities combined).</summary>
    public int? FacilityId { get; set; }

    /// <summary>DHIS2 Organisation Unit UID for the target facility.</summary>
    [MaxLength(11)]
    public string? OrgUnitUid { get; set; }

    /// <summary>Daily | Weekly | Monthly</summary>
    [MaxLength(10)]
    public string PeriodType { get; set; } = "Monthly";

    public bool IsActive { get; set; } = true;
}
