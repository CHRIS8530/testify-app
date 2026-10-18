using Xunit;

namespace Testify.Api.Tests;

public class CorsTests : IClassFixture<TestifyApiFactory>
{
    private readonly HttpClient _client;

    public CorsTests(TestifyApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private Task<HttpResponseMessage> Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/projects");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Preflight_FromConfiguredOrigin_IsAllowed()
    {
        var response = await Preflight(TestifyApiFactory.AllowedOrigin);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(TestifyApiFactory.AllowedOrigin, Assert.Single(values));
    }

    [Fact]
    public async Task Preflight_FromAnyOtherOrigin_IsNotAllowed()
    {
        var response = await Preflight("http://evil.test");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
