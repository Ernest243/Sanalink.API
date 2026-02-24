using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public ApplicationUser Doctor { get; set; } = default!;

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
