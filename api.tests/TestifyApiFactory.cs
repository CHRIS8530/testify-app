using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
    }
}
