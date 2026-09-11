using Testify.Api.Services;

namespace Testify.Api.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var result = jwtService.ValidateToken(token);

            if (result.HasValue)
            {
                context.Items["User"] = new
                {
                    UserId = result.Value.UserId,
                    Email = result.Value.Email,
                    Username = result.Value.Username
                };

                // Also set claims on the principal so [Authorize] attributes work
                var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
                {
                    new(System.Security.Claims.ClaimTypes.NameIdentifier, result.Value.UserId.ToString()),
                    new(System.Security.Claims.ClaimTypes.Email, result.Value.Email),
                    new(System.Security.Claims.ClaimTypes.Name, result.Value.Username)
                };

                var identity = new System.Security.Claims.ClaimsIdentity(claims, "Bearer");
                context.User = new System.Security.Principal.GenericPrincipal(identity, null);
            }
        }

        await _next(context);
    }
}