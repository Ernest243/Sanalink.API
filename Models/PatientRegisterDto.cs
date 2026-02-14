using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Models
{
    public class PatientRegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;

        [Required]
        public string FirstName { get; set; } = default!;

        [Required]
        public string LastName { get; set; } = default!;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = default!;

        [Required]
        public int FacilityId { get; set; }
    }
}
