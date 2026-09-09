using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();

// Configure DbContext with conditional provider (InMemory for tests, PostgreSQL for production)
if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<TestifyDbContext>(options =>
        options.UseInMemoryDatabase("TestifyDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "DefaultConnection";
    builder.Services.AddDbContext<TestifyDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations automatically (skip for InMemory tests)
if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();
        db.Database.Migrate();
    }
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Health check endpoint - works with both Npgsql and InMemory
app.MapGet("/api/v1/health", async (TestifyDbContext db) =>
{
    try
    {
        if (db.Database.IsInMemory())
        {
            await db.Database.EnsureCreatedAsync();
            return Results.Ok(new { status = "ok", database = "connected" });
        }

        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        return Results.Ok(new { status = "ok", database = "connected" });
    }
    catch (Exception)
    {
        return Results.StatusCode(503);
    }
});

app.MapControllers();
app.Run();