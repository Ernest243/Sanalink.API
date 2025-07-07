namespace Sanalink.API.DTOs
{
    public class NoteReadDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
