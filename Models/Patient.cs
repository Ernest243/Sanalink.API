namespace Sanalink.API.Models;

public class Patient
{
    public int Id { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; } // User ID
}
