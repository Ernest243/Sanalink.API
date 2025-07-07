namespace Sanalink.API.DTOs
{
    public class PrescriptionCreateDto
    {
        public int PatientId { get; set; }
        public string MedicationName { get; set; } = default!;
        public string Dosage { get; set; } = default!;
        public string Instructions { get; set; } = default!;
    }
}
