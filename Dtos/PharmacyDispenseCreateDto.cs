namespace Sanalink.API.DTOs
{
    public class PharmacyDispenseCreateDto
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public string MedicationName { get; set; } = default!;
        public string QuantityDispensed { get; set; } = default!;
        public string? Notes { get; set; }
    }
}
