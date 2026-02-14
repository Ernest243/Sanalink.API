using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class PharmacyDispense
    {
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [ForeignKey("PrescriptionId")]
        public Prescription Prescription { get; set; } = default!;

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = default!;

        [Required]
        public string DispensedById { get; set; } = default!;

        [ForeignKey("DispensedById")]
        public ApplicationUser DispensedBy { get; set; } = default!;

        [Required]
        [MaxLength(255)]
        public string MedicationName { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string QuantityDispensed { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime? DispensedAt { get; set; }

        public DateTime? CollectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
