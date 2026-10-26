using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testify.Api.Services;

namespace Testify.Api.Tests;

public class TestifyApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://allowed.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Program.cs reads this while it is still registering services,
        // so it has to go in with UseSetting (applied before Program.cs runs).
        builder.UseSetting("CORS_ALLOWED_ORIGINS", AllowedOrigin);

        // JwtService reads this later, once the app is built, so this way works.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", "test-secret-key-for-integration-tests-only-not-for-production-use" }
            });
        });

        // The rate limiter would answer 429 once the tests make more than 100 requests in a minute.
        // Rate limiting gets its own tests later.
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

        // Emails are captured in memory instead of being logged.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<CapturingEmailService>();
            services.AddScoped<IEmailService>(provider => provider.GetRequiredService<CapturingEmailService>());
        });
    }
}
