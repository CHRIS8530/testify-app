using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Middleware;
using Testify.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<TestifyDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Test"))
{
    app.Services.GetRequiredService<TestifyDbContext>().Database.Migrate();
}

app.Run();