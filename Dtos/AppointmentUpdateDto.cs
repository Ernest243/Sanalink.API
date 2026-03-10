namespace Sanalink.API.Dtos;

public class AppointmentUpdateDto
{
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
