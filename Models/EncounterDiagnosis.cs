using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models;

public class EncounterDiagnosis
{
    public int Id { get; set; }

    [Required]
    public int EncounterId { get; set; }

    [ForeignKey("EncounterId")]
    public Encounter Encounter { get; set; } = default!;

    /// <summary>ICD-10 / CIM-10 code, e.g. "J18.9"</summary>
    [Required]
    [MaxLength(10)]
    public string ICD10Code { get; set; } = default!;

    /// <summary>Human-readable label, e.g. "Pneumonie, sans précision"</summary>
    [Required]
    [MaxLength(500)]
    public string ICD10Description { get; set; } = default!;

    /// <summary>Primary | Secondary | Comorbidity</summary>
    [MaxLength(20)]
    public string DiagnosisType { get; set; } = "Primary";

    /// <summary>Ordering within the encounter (1 = most important)</summary>
    public int Rank { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
