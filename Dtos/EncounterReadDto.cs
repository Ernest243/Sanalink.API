namespace Sanalink.API.DTOs
{
    public class EncounterReadDto
    {
        public int Id { get; set; }
        public string EncounterNumber { get; set; } = default!;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public string? NurseName { get; set; }
        public string Status { get; set; } = default!;
        public string ChiefComplaint { get; set; } = default!;
        public string? Vitals { get; set; }
        public string? Diagnosis { get; set; }
        public string? ClinicalNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
