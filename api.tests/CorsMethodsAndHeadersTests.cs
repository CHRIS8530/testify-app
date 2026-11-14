using Xunit;

namespace Testify.Api.Tests;

public class CorsMethodsAndHeadersTests : IClassFixture<TestifyApiFactory>
{
    private readonly HttpClient _client;

    public CorsMethodsAndHeadersTests(TestifyApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private Task<HttpResponseMessage> Preflight(string method, string? headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/projects");
        request.Headers.TryAddWithoutValidation("Origin", TestifyApiFactory.AllowedOrigin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", method);
        if (headers is not null)
            request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", headers);
        return _client.SendAsync(request);
    }

    // These pass before and after the change on purpose. They prove the narrowing does not lock out what the app uses.
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Preflight_ForTheMethodsTheAppUses_IsAllowed(string method)
    {
        var response = await Preflight(method);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_ForTheHeadersTheAppSends_IsAllowed()
    {
        var response = await Preflight("POST", "authorization,content-type");

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_ForAMethodTheAppNeverUses_IsNotAllowed()
    {
        var response = await Preflight("TRACE");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_ForAnUnexpectedHeader_IsNotAllowed()
    {
        var response = await Preflight("POST", "x-custom-header");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}