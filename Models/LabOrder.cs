using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class LabOrder
    {
        public int Id { get; set; }

        [Required]
        public int EncounterId { get; set; }

        [ForeignKey("EncounterId")]
        public Encounter Encounter { get; set; } = default!;

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = default!;

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public ApplicationUser Doctor { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string TestName { get; set; } = default!;

        [MaxLength(100)]
        public string? TestCategory { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(20)]
        public string Priority { get; set; } = "Routine";

        [MaxLength(1000)]
        public string? ClinicalNotes { get; set; }

        public string? Result { get; set; }

        [MaxLength(1000)]
        public string? ResultNotes { get; set; }

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}
