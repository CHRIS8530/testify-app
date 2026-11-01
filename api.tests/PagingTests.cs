using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class PagingTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public PagingTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("offset=-5")]
    [InlineData("limit=-1")]
    [InlineData("limit=0")]
    [InlineData("limit=201")]
    public async Task GetTestCases_WithAnInvalidPagingValue_ReturnsBadRequest(string query)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTestCases_WithTheMaximumLimit_ReturnsOk()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases?limit=200&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // This passes before and after the change on purpose. It guards the sort order the change adds.
    [Fact]
    public async Task GetTestCases_ListsTheOldestFirst()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var first = await CreateCaseAsync(alice, projectId);
        await Task.Delay(50);
        var second = await CreateCaseAsync(alice, projectId);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(first, cases[0].GetProperty("id").GetGuid());
        Assert.Equal(second, cases[1].GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CreateCaseAsync(TestUser user, Guid projectId)
    {
        var response = await ApiTestHelpers.PostTestCaseAsync(user, projectId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}