namespace Sanalink.API.DTOs
{
    public class FacilityCreateDto
    {
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
