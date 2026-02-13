using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sanalink.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Role { get; set; } // Doctor, Nurse, Admin
        public string? Department { get; set; } // e.g., Cardiology, Pediatrics
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public int? FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }
    }
}