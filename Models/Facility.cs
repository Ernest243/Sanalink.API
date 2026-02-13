using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Models
{
    public class Facility
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
