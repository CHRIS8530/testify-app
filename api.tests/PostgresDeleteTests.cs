using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class PostgresDeleteTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PostgresDeleteTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // The in-memory database does not enforce foreign keys. On PostgreSQL the result row points at the test case
    // with ON DELETE RESTRICT, so the delete used to fail with a constraint violation and an unhandled exception.
    [SkippableFact]
    public async Task OnARealPostgres_DeletingATestCaseThatIsInARun_ReturnsConflict()
    {
        Skip.IfNot(_factory.DockerAvailable, "Docker is not available on this machine. This test runs in CI.");

        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var created = await ApiTestHelpers.PostTestCaseAsync(alice, projectId);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var caseId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var run = await alice.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, run.StatusCode);
        var runId = (await run.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var added = await alice.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = new[] { caseId } });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}