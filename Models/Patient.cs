using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sanalink.API.Models;

public class Patient
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = default!;

    public string? MiddleName { get; set; }

    [Required]
    public string LastName { get; set; } = default!;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public string Gender { get; set; } = default!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    public int FacilityId { get; set; }

    [ForeignKey("FacilityId")]
    public Facility? Facility { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
}
