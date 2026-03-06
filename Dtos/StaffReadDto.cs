namespace Sanalink.API.DTOs
{
    public class StaffReadDto
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Role { get; set; }
        public string? Department { get; set; }
        public int? FacilityId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
