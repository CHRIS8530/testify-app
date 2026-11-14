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

    // The values of a response header, split on commas, so "GET,POST" becomes two entries.
    private static string[] Values(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values)
            ? values.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToArray()
            : Array.Empty<string>();
    }

    // Shown when a test fails, so the failure says what the server really sent.
    private static string Describe(HttpResponseMessage response)
    {
        return "Headers: " + string.Join("; ", response.Headers.Select(header => $"{header.Key}={string.Join(",", header.Value)}"));
    }

    // The middleware sends Access-Control-Allow-Origin for an allowed origin whatever the method or headers are.
    // What decides whether the browser goes ahead is Access-Control-Allow-Methods and Access-Control-Allow-Headers.

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

        Assert.True(Values(response, "Access-Control-Allow-Methods").Contains(method, StringComparer.OrdinalIgnoreCase), Describe(response));
    }

    [Fact]
    public async Task Preflight_ForTheHeadersTheAppSends_IsAllowed()
    {
        var response = await Preflight("POST", "authorization,content-type");

        var allowed = Values(response, "Access-Control-Allow-Headers");
        Assert.True(allowed.Contains("authorization", StringComparer.OrdinalIgnoreCase), Describe(response));
        Assert.True(allowed.Contains("content-type", StringComparer.OrdinalIgnoreCase), Describe(response));
    }

    [Fact]
    public async Task Preflight_ForAMethodTheAppNeverUses_IsNotAllowed()
    {
        var response = await Preflight("TRACE");

        Assert.False(Values(response, "Access-Control-Allow-Methods").Contains("TRACE", StringComparer.OrdinalIgnoreCase), Describe(response));
    }

    [Fact]
    public async Task Preflight_ForAnUnexpectedHeader_IsNotAllowed()
    {
        var response = await Preflight("POST", "x-custom-header");

        Assert.False(Values(response, "Access-Control-Allow-Headers").Contains("x-custom-header", StringComparer.OrdinalIgnoreCase), Describe(response));
    }
}