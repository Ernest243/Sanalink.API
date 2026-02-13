namespace Sanalink.API.DTOs
{
    public class LabOrderCreateDto
    {
        public int EncounterId { get; set; }
        public int PatientId { get; set; }
        public string TestName { get; set; } = default!;
        public string? TestCategory { get; set; }
        public string? Priority { get; set; }
        public string? ClinicalNotes { get; set; }
    }
}
