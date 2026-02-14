using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Models
{
    public class StaffRegisterDto
    {
        [Required]
        public string FullName { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;

        [Required]
        public string Role { get; set; } = default!;

        public string? Department { get; set; }

        public int? FacilityId { get; set; }
    }
}
