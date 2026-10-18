using Xunit;

namespace Testify.Api.Tests;

public class ProjectsControllerTests : IClassFixture<TestifyApiFactory>
{
    private readonly HttpClient _client;

    public ProjectsControllerTests(TestifyApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProjects_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/projects");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { name = "Test Project" };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/projects", content);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_WithNonexistentId_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/projects/{projectId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
