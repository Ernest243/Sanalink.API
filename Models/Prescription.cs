using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public ApplicationUser Doctor { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string MedicationName { get; set; } = default!;

        [MaxLength(255)]
        public string Dosage { get; set; } = default!;

        [MaxLength(500)]
        public string Instructions { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
