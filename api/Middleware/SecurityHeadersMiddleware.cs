namespace Testify.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS: Strict-Transport-Security
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // X-Content-Type-Options: prevents MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options: prevents clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection: legacy XSS protection
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Content-Security-Policy: restricts resource loading
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";

        // Referrer-Policy: controls referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        await _next(context);
    }
}