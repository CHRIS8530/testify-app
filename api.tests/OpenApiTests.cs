using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class OpenApiTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public OpenApiTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<JsonElement> GetDocumentAsync()
    {
        var response = await _factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task OpenApiDocument_IsServedAsJsonWithoutSigningIn()
    {
        var response = await _factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DeclaresAnOpenApiVersion()
    {
        var document = await GetDocumentAsync();

        Assert.StartsWith("3.", document.GetProperty("openapi").GetString());
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/projects")]
    [InlineData("/api/v1/projects/{projectId}/test-runs")]
    [InlineData("/api/v1/projects/{projectId}/dashboard")]
    public async Task OpenApiDocument_ListsTheMainEndpoints(string path)
    {
        var document = await GetDocumentAsync();

        Assert.True(document.GetProperty("paths").TryGetProperty(path, out _), $"{path} is missing from the OpenAPI document");
    }
}