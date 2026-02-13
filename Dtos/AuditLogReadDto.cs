namespace Sanalink.API.DTOs
{
    public class AuditLogReadDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = default!;
        public string Endpoint { get; set; } = default!;
        public string? RequestBody { get; set; }
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
