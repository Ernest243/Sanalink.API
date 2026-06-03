using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Sanalink.API.Services;

namespace Sanalink.API.Middleware;

/// <summary>
/// Rejects requests whose JWT has been revoked via POST /api/v1/auth/logout.
/// Only active when the ISO27001_TokenRevocation feature flag is on.
/// </summary>
public class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRevocationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (!string.IsNullOrEmpty(jti))
            {
                var revocationSvc = context.RequestServices
                    .GetRequiredService<ITokenRevocationService>();

                if (await revocationSvc.IsRevokedAsync(jti))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Token has been revoked. Please log in again." });
                    return;
                }
            }
        }

        await _next(context);
    }
}
