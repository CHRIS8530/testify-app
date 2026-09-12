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

if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<TestifyDbContext>(options =>
    {
        options.UseInMemoryDatabase("TestDB");
    });
}
else
{
    builder.Services.AddDbContext<TestifyDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

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
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();
    db.Database.Migrate();
}

app.Run();