using System.Security.Claims;
using System.Text;
using Sanalink.API.Data;
using Sanalink.API.Models;

namespace Sanalink.API.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly HashSet<string> TrackedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "PATCH", "DELETE", "GET"
        };

        public AuditLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!TrackedMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Buffer request body
            context.Request.EnableBuffering();
            string? requestBody = null;
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // Buffer response body to capture it
            var originalResponseBody = context.Response.Body;
            using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            await _next(context);

            // Read and restore response body
            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            // Extract JWT claims
            var userId   = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email    = context.User.FindFirstValue(ClaimTypes.Email);
            var role     = context.User.FindFirstValue(ClaimTypes.Role);
            var facility = context.User.FindFirstValue("facilityId");

            // Parse resource + resourceId from path: /api/v1/{Resource}/{id?}
            var segments = context.Request.Path.Value?
                .Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var resource   = segments.Length > 2 ? segments[2] : null;
            var resourceId = segments.Length > 3 && int.TryParse(segments[3], out _)
                ? segments[3] : null;

            // Map HTTP method to semantic action
            var action = context.Request.Method.ToUpper() switch
            {
                "POST"   => "CREATE",
                "PUT"    => "UPDATE",
                "PATCH"  => "UPDATE",
                "DELETE" => "DELETE",
                "GET"    => "VIEW",
                _        => context.Request.Method.ToUpper()
            };

            // Capture old/new values for mutations
            string? oldValue = null;
            string? newValue = null;
            if (action is "UPDATE" or "DELETE")
            {
                oldValue = string.IsNullOrWhiteSpace(requestBody) ? null : requestBody;
                newValue = string.IsNullOrWhiteSpace(responseBody) ? null : responseBody;
            }
            else if (action == "CREATE")
            {
                newValue = string.IsNullOrWhiteSpace(responseBody) ? null : responseBody;
            }

            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500) userAgent = userAgent[..500];

            var auditLog = new AuditLog
            {
                UserId     = userId,
                UserEmail  = email,
                UserRole   = role,
                FacilityId = facility,
                Action     = action,
                Resource   = resource,
                ResourceId = resourceId,
                Endpoint   = context.Request.Path.ToString(),
                OldValue   = oldValue,
                NewValue   = newValue,
                StatusCode = context.Response.StatusCode,
                IpAddress  = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent  = userAgent,
                Timestamp  = DateTime.UtcNow
            };

            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync();
        }
    }
}
