namespace Sanalink.API.Dtos;

public class PatientReadDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = default!;
    public string FullName => MiddleName is null
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int FacilityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
