using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class Encounter
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string EncounterNumber { get; set; } = default!;

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = default!;

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public ApplicationUser Doctor { get; set; } = default!;

        public string? NurseId { get; set; }

        [ForeignKey("NurseId")]
        public ApplicationUser? Nurse { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        [Required]
        [MaxLength(500)]
        public string ChiefComplaint { get; set; } = default!;

        public string? Vitals { get; set; }

        [MaxLength(1000)]
        public string? Diagnosis { get; set; }

        [MaxLength(2000)]
        public string? ClinicalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }
    }
}
