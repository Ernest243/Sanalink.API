using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [MaxLength(256)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        [MaxLength(50)]
        public string? FacilityId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = default!;

        [MaxLength(100)]
        public string? Resource { get; set; }

        [MaxLength(50)]
        public string? ResourceId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; } = default!;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public int StatusCode { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
