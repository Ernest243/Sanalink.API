namespace Sanalink.API.DTOs
{
    public class EncounterCreateDto
    {
        public int PatientId { get; set; }
        public string ChiefComplaint { get; set; } = default!;
        public string? Vitals { get; set; }
    }
}
