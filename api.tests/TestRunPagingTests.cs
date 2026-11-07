using System.Net;
using Xunit;

namespace Testify.Api.Tests;

public class TestRunPagingTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public TestRunPagingTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("offset=-5")]
    [InlineData("limit=-1")]
    [InlineData("limit=0")]
    [InlineData("limit=201")]
    public async Task GetTestRuns_WithAnInvalidPagingValue_ReturnsBadRequest(string query)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Passes before and after the change on purpose. It proves the fix does not reject the largest allowed page.
    [Fact]
    public async Task GetTestRuns_WithTheMaximumLimit_ReturnsOk()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs?limit=200&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}