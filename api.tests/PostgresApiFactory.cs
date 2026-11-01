using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Testify.Api.Tests;

// Runs the real API against a real PostgreSQL in a throwaway Docker container.
// Any environment name except "Test" makes Program.cs use Npgsql and apply the real migrations on startup.
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _database;

    public bool DockerAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Built here and not in a field initializer: Build() checks for Docker and throws when it is missing.
            _database = new PostgreSqlBuilder("postgres:15-alpine").Build();
            await _database.StartAsync();
            DockerAvailable = true;
        }
        catch (Exception) when (Environment.GetEnvironmentVariable("CI") != "true")
        {
            // On a machine without Docker the tests skip themselves.
            // In CI (GitHub sets CI=true) the exception is not caught, so a missing Docker fails the run instead of hiding it.
            DockerAvailable = false;
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (_database is not null)
            await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_database is null)
            throw new InvalidOperationException("The database container is not running. Tests must skip when DockerAvailable is false.");

        builder.UseEnvironment("Integration");

        // Program.cs reads these while it registers services, so they go in with UseSetting.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
        builder.UseSetting("CORS_ALLOWED_ORIGINS", "http://allowed.test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", "test-secret-key-for-integration-tests-only-not-for-production-use" }
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<IpRateLimitOptions>(options =>
            {
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 1000000 }
                };
            });
        });
    }
}