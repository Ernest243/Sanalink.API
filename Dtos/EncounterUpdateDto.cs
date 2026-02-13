namespace Sanalink.API.DTOs
{
    public class EncounterUpdateDto
    {
        public string? ChiefComplaint { get; set; }
        public string? Vitals { get; set; }
        public string? Diagnosis { get; set; }
        public string? ClinicalNotes { get; set; }
        public string? NurseId { get; set; }
    }
}
