using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class ProjectPagingTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public ProjectPagingTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("offset=-5")]
    [InlineData("limit=-1")]
    [InlineData("limit=0")]
    [InlineData("limit=201")]
    public async Task GetProjects_WithAnInvalidPagingValue_ReturnsBadRequest(string query)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await alice.Client.GetAsync($"/api/v1/projects?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Passes before and after the change on purpose. It proves the fix does not reject the largest allowed page.
    [Fact]
    public async Task GetProjects_WithTheMaximumLimit_ReturnsOk()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await alice.Client.GetAsync("/api/v1/projects?limit=200&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ListsTheNewestProjectFirst()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        await ApiTestHelpers.CreateProjectAsync(alice);
        await Task.Delay(50);
        var newest = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(newest, projects[0].GetProperty("id").GetGuid());
    }
}