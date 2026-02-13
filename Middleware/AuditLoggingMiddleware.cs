using System.Security.Claims;
using Sanalink.API.Data;
using Sanalink.API.Models;

namespace Sanalink.API.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "DELETE"
        };

        public AuditLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!MutatingMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Enable buffering so the request body can be read multiple times
            context.Request.EnableBuffering();

            string? requestBody = null;
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // Execute the rest of the pipeline
            await _next(context);

            // After the response, capture audit data
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500) userAgent = userAgent[..500];

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = context.Request.Method,
                Endpoint = context.Request.Path.ToString(),
                RequestBody = string.IsNullOrWhiteSpace(requestBody) ? null : requestBody,
                StatusCode = context.Response.StatusCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            // Use a new scope to avoid conflicts with the request's DbContext
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AuditLogs.Add(auditLog);
            await dbContext.SaveChangesAsync();
        }
    }
}
