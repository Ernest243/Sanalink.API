namespace Sanalink.API.Models
{
    public class RegisterDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public int? FacilityId { get; set; }
    }
}