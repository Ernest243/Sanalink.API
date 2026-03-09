namespace Sanalink.API.DTOs
{
    public class AuditLogReadDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? FacilityId { get; set; }
        public string Action { get; set; } = default!;
        public string? Resource { get; set; }
        public string? ResourceId { get; set; }
        public string Endpoint { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
