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

        public async Task<PagedResult<AuditLogReadDto>> QueryLogsAsync(AuditLogQueryDto query)
        {
            var q = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
                q = q.Where(a => a.UserId == query.UserId);

            if (!string.IsNullOrWhiteSpace(query.UserEmail))
                q = q.Where(a => a.UserEmail != null &&
                    a.UserEmail.ToLower().Contains(query.UserEmail.ToLower()));

            if (!string.IsNullOrWhiteSpace(query.Resource))
                q = q.Where(a => a.Resource != null &&
                    a.Resource.ToLower() == query.Resource.ToLower());

            if (!string.IsNullOrWhiteSpace(query.Action))
                q = q.Where(a => a.Action.ToLower() == query.Action.ToLower());

            if (query.StatusCode.HasValue)
                q = q.Where(a => a.StatusCode == query.StatusCode.Value);

            if (query.From.HasValue)
                q = q.Where(a => a.Timestamp >= query.From.Value.ToUniversalTime());

            if (query.To.HasValue)
                q = q.Where(a => a.Timestamp <= query.To.Value.ToUniversalTime());

            var total = await q.CountAsync();

            var pageSize = Math.Clamp(query.PageSize, 1, 200);
            var page     = Math.Max(query.Page, 1);

            var items = await q
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToReadDto(a))
                .ToListAsync();

            return new PagedResult<AuditLogReadDto>
            {
                Total    = total,
                Page     = page,
                PageSize = pageSize,
                Items    = items
            };
        }

        private static AuditLogReadDto MapToReadDto(Models.AuditLog a)
        {
            return new AuditLogReadDto
            {
                Id         = a.Id,
                UserId     = a.UserId,
                UserName   = a.User != null ? a.User.FullName ?? a.User.UserName : null,
                UserEmail  = a.UserEmail,
                UserRole   = a.UserRole,
                FacilityId = a.FacilityId,
                Action     = a.Action,
                Resource   = a.Resource,
                ResourceId = a.ResourceId,
                Endpoint   = a.Endpoint,
                OldValue   = a.OldValue,
                NewValue   = a.NewValue,
                StatusCode = a.StatusCode,
                IpAddress  = a.IpAddress,
                Timestamp  = a.Timestamp
            };
        }
    }
}
