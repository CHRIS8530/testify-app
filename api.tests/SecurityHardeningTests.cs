using System.Text.Json;
using Testify.Api.Models;
using Xunit;

namespace Testify.Api.Tests;

public class SecurityHardeningTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public SecurityHardeningTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void SerializingAUser_NeverIncludesThePasswordHash()
    {
        var user = new User { Email = "someone@example.com", Username = "someone", PasswordHash = "secret-hash-value" };

        var json = JsonSerializer.Serialize(user);

        Assert.DoesNotContain("secret-hash-value", json);
    }

    [Fact]
    public async Task Health_NeverReturnsExceptionDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("error", body, StringComparison.OrdinalIgnoreCase);
    }
}
