namespace Sanalink.API.DTOs
{
    public class PrescriptionReadDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string MedicationName { get; set; } = default!;
        public string Dosage { get; set; } = default!;
        public string Instructions { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
