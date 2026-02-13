namespace Sanalink.API.DTOs
{
    public class LabOrderReadDto
    {
        public int Id { get; set; }
        public int EncounterId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public string TestName { get; set; } = default!;
        public string? TestCategory { get; set; }
        public string Status { get; set; } = default!;
        public string Priority { get; set; } = default!;
        public string? ClinicalNotes { get; set; }
        public string? Result { get; set; }
        public string? ResultNotes { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
