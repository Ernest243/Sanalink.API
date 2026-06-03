namespace Sanalink.API.DTOs;

public class EncounterCreateDto
{
    public int PatientId { get; set; }
    public string ChiefComplaint { get; set; } = default!;
    public string? Vitals { get; set; }

    /// <summary>ICD-10 coded diagnoses. Only persisted when the ICD10 feature flag is enabled.</summary>
    public List<EncounterDiagnosisCreateDto>? Diagnoses { get; set; }
}
