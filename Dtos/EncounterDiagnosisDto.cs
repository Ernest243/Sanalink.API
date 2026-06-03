using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.DTOs;

public class EncounterDiagnosisCreateDto
{
    [Required]
    [MaxLength(10)]
    public string ICD10Code { get; set; } = default!;

    [Required]
    [MaxLength(500)]
    public string ICD10Description { get; set; } = default!;

    /// <summary>Primary | Secondary | Comorbidity</summary>
    [MaxLength(20)]
    public string DiagnosisType { get; set; } = "Primary";

    public int Rank { get; set; } = 1;
}

public class EncounterDiagnosisReadDto
{
    public int Id { get; set; }
    public string ICD10Code { get; set; } = default!;
    public string ICD10Description { get; set; } = default!;
    public string DiagnosisType { get; set; } = default!;
    public int Rank { get; set; }
}
