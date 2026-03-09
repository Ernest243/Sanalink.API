namespace Sanalink.API.DTOs
{
    public class AuditLogQueryDto
    {
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public int? StatusCode { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class PagedResult<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        public List<T> Items { get; set; } = [];
    }
}
