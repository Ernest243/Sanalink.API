namespace Sanalink.API.DTOs
{
    public class NoteCreateDto
    {
        public int PatientId { get; set; }
        public string Content { get; set; } = default!;
    }
}
