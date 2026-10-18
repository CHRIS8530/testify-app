using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Middleware;
using Testify.Api.Services;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Rate limiting - ORDER MATTERS
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100
        }
    };
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Origins the browser may call the API from, as a comma separated list.
// Read here, while services are being registered, because the CORS policy is built now.
var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "CORS_ALLOWED_ORIGINS is not configured. Set it to a comma separated list of frontend origins, for example http://localhost:5173.");

if (allowedOrigins.Contains("*"))
    throw new InvalidOperationException("CORS_ALLOWED_ORIGINS must list explicit origins, not *.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<TestifyDbContext>(options =>
    {
        options.UseInMemoryDatabase("TestDB");
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings__DefaultConnection is not configured. Set it with 'dotnet user-secrets' locally or as an environment variable.");

    builder.Services.AddDbContext<TestifyDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });
}

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseIpRateLimiting();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Test"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();
    db.Database.Migrate();
}

app.Run();