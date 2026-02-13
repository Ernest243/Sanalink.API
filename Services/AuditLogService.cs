using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AuditLogReadDto>> GetAllLogsAsync()
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => MapToReadDto(a))
                .ToListAsync();
        }

        public async Task<List<AuditLogReadDto>> GetLogsByUserAsync(string userId)
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => MapToReadDto(a))
                .ToListAsync();
        }

        private static AuditLogReadDto MapToReadDto(Models.AuditLog a)
        {
            return new AuditLogReadDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.FullName ?? a.User.UserName : null,
                Action = a.Action,
                Endpoint = a.Endpoint,
                RequestBody = a.RequestBody,
                StatusCode = a.StatusCode,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp
            };
        }
    }
}
