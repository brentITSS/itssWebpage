using backend.Services;
using System.Security.Claims;

namespace backend.Middleware;

public class WorkstreamAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WorkstreamAuthorizationMiddleware> _logger;

    public WorkstreamAuthorizationMiddleware(RequestDelegate next, ILogger<WorkstreamAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Skip authorization check if the endpoint doesn't require authentication
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Get the requested workstream from route or query
        var path = context.Request.Path.Value ?? "";
        
        // Check if this is a Property Hub endpoint
        if (path.Contains("/api/properties", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/tenants", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/journals", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/contact-logs", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/api/tags", StringComparison.OrdinalIgnoreCase))
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var user = await authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Global Admin has access to everything
            if (authService.IsGlobalAdmin(user))
            {
                await _next(context);
                return;
            }

            // Check if user has Property Hub workstream access
            // Assuming Property Hub has a specific workstream ID (you may need to look this up)
            // For now, we'll check if they have any workstream access
            if (!user.WorkstreamAccess.Any())
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied: No workstream access");
                return;
            }

            // For Property Hub, you might want to check for a specific workstream ID
            // This is a simplified check - you may need to refine based on your workstream IDs
            var hasPropertyHubAccess = user.WorkstreamAccess.Any(wa => 
                wa.WorkstreamName.Contains("Property Hub", StringComparison.OrdinalIgnoreCase) ||
                wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase));

            if (!hasPropertyHubAccess)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied: Property Hub workstream access required");
                return;
            }
        }

        await _next(context);
    }
}
