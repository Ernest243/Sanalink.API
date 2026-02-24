namespace Sanalink.API.Models;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string DoctorId { get; set; } = default!;
    public ApplicationUser? Doctor { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; } = default!;
    public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
}