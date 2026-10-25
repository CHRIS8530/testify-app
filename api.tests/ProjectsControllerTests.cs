using System.Net;
using System.Text;
using Xunit;

namespace Testify.Api.Tests;

public class ProjectsControllerTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public ProjectsControllerTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_WhenSignedIn_ReturnsOk()
    {
        var user = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await user.Client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { name = "Test Project" };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _factory.CreateClient().PostAsync("/api/v1/projects", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_WithNonexistentId_ReturnsNotFound()
    {
        var user = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await user.Client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
