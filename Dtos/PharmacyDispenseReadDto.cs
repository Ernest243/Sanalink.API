namespace Sanalink.API.DTOs
{
    public class PharmacyDispenseReadDto
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = default!;
        public string DispensedByName { get; set; } = default!;
        public string MedicationName { get; set; } = default!;
        public string QuantityDispensed { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime? DispensedAt { get; set; }
        public DateTime? CollectedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
