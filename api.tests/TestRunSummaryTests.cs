using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class TestRunSummaryTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public TestRunSummaryTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> CreateCaseAsync(TestUser user, Guid projectId)
    {
        var response = await ApiTestHelpers.PostTestCaseAsync(user, projectId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateRunAsync(TestUser user, Guid projectId)
    {
        var response = await user.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // A run with three cases: one Pass, one Fail and one left as Not Run.
    private static async Task<Guid> CreateRunWithThreeResultsAsync(TestUser user, Guid projectId)
    {
        var caseIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
            caseIds.Add(await CreateCaseAsync(user, projectId));

        var runId = await CreateRunAsync(user, projectId);

        var added = await user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = caseIds });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var resultIds = (await added.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Select(result => result.GetProperty("id").GetGuid())
            .ToList();

        await SetResultAsync(user, projectId, runId, resultIds[0], "Pass");
        await SetResultAsync(user, projectId, runId, resultIds[1], "Fail");
        return runId;
    }

    private static async Task SetResultAsync(TestUser user, Guid projectId, Guid runId, Guid resultId, string outcome)
    {
        var response = await user.Client.PatchAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results/{resultId}", new { result = outcome, notes = "" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void AssertThreeResultSummary(JsonElement run)
    {
        Assert.Equal(3, run.GetProperty("total").GetInt32());
        Assert.Equal(1, run.GetProperty("passed").GetInt32());
        Assert.Equal(1, run.GetProperty("failed").GetInt32());
        Assert.Equal(0, run.GetProperty("blocked").GetInt32());
        Assert.Equal(1, run.GetProperty("notRun").GetInt32());
        Assert.Equal(33, run.GetProperty("passRate").GetInt32());
    }

    [Fact]
    public async Task GetTestRuns_IncludesResultCountsAndPassRate()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var runId = await CreateRunWithThreeResultsAsync(alice, projectId);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var runs = await response.Content.ReadFromJsonAsync<JsonElement>();
        AssertThreeResultSummary(runs.EnumerateArray().Single(run => run.GetProperty("id").GetGuid() == runId));
    }

    [Fact]
    public async Task GetTestRun_IncludesResultCountsAndPassRate()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var runId = await CreateRunWithThreeResultsAsync(alice, projectId);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs/{runId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertThreeResultSummary(await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    [Fact]
    public async Task GetTestRuns_ForARunWithNoResults_ReturnsZeros()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await CreateRunAsync(alice, projectId);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs");

        var run = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
        Assert.Equal(0, run.GetProperty("total").GetInt32());
        Assert.Equal(0, run.GetProperty("passRate").GetInt32());
    }

    [Fact]
    public async Task GetTestRuns_ListsTheNewestRunFirst()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await CreateRunAsync(alice, projectId);
        var newest = await CreateRunAsync(alice, projectId);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs");

        var runs = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(newest, runs[0].GetProperty("id").GetGuid());
    }
}