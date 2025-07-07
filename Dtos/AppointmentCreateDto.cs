namespace Sanalink.API.Dtos;

public class AppointmentCreateDto
{
    public int PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
}