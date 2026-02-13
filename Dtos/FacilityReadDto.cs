namespace Sanalink.API.DTOs
{
    public class FacilityReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
