using System.ComponentModel.DataAnnotations;

namespace Sanalink.API.Dtos;

public class PatientCreateDto
{
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
}
