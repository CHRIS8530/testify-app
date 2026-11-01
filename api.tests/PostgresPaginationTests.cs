using System.Net;
using Xunit;

namespace Testify.Api.Tests;

public class PostgresPaginationTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PostgresPaginationTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // The in-memory database accepts a negative offset. PostgreSQL rejects OFFSET -5, so the API crashed with a 500.
    // A bad query value is the caller's mistake, so the API should answer 400.
    [SkippableTheory]
    [InlineData("offset=-5")]
    [InlineData("limit=-1")]
    public async Task GetTestCases_WithANegativePagingValue_ReturnsBadRequest(string query)
    {
        Skip.IfNot(_factory.DockerAvailable, "Docker is not available on this machine. This test runs in CI.");

        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}