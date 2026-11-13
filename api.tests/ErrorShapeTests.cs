using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class ErrorShapeTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public ErrorShapeTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    // Every error body looks like { "error": { "message", "code" }, "status": 4xx }, with nothing from ProblemDetails.
    private static async Task AssertErrorShapeAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = body.GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.False(body.TryGetProperty("traceId", out _));
        Assert.False(body.TryGetProperty("title", out _));
    }

    [Fact]
    public async Task MalformedJson_ReturnsTheStandardErrorShape()
    {
        var content = new StringContent("{ this is not json", Encoding.UTF8, "application/json");

        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/login", content);

        await AssertErrorShapeAsync(response, 400, "INVALID_REQUEST");
    }

    [Fact]
    public async Task AnIdThatIsNotAGuid_ReturnsTheStandardErrorShape()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/projects/not-a-guid");

        await AssertErrorShapeAsync(response, 400, "INVALID_REQUEST");
    }

    [Fact]
    public async Task ANotFoundFromAController_ReturnsTheStandardErrorShape()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases/{Guid.NewGuid()}");

        await AssertErrorShapeAsync(response, 404, "NOT_FOUND");
    }

    // Passes before and after on purpose. It proves an error the API already shaped is left alone.
    [Fact]
    public async Task AWrongPassword_KeepsItsOwnErrorShape()
    {
        var user = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Wrong!Passw0rd1" });

        await AssertErrorShapeAsync(response, 401, "INVALID_CREDENTIALS");
    }
}