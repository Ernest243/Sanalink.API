using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface IAuditLogService
    {
        Task<List<AuditLogReadDto>> GetAllLogsAsync();
        Task<List<AuditLogReadDto>> GetLogsByUserAsync(string userId);
    }
}
