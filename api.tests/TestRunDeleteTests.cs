using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class TestRunDeleteTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public TestRunDeleteTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    // A run with one case in it. The result is marked Fail when a defect is going to be raised from it.
    private static async Task<(Guid RunId, Guid CaseId, Guid ResultId)> NewRunWithOneResultAsync(TestUser user, Guid projectId, string outcome)
    {
        var caseResponse = await ApiTestHelpers.PostTestCaseAsync(user, projectId);
        Assert.Equal(HttpStatusCode.Created, caseResponse.StatusCode);
        var caseId = (await caseResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var run = await user.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, run.StatusCode);
        var runId = (await run.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var added = await user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = new[] { caseId } });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var resultId = (await added.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First().GetProperty("id").GetGuid();

        var marked = await user.Client.PatchAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results/{resultId}", new { result = outcome, notes = "" });
        Assert.Equal(HttpStatusCode.OK, marked.StatusCode);

        return (runId, caseId, resultId);
    }

    private static async Task RaiseDefectAsync(TestUser user, Guid projectId, Guid caseId, Guid resultId)
    {
        var created = await user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId = caseId, testRunResultId = resultId, title = "Login button does nothing", description = "", severity = "High" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task DeleteTestRun_ThatHasADefect_ReturnsConflict()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (runId, caseId, resultId) = await NewRunWithOneResultAsync(alice, projectId, "Fail");
        await RaiseDefectAsync(alice, projectId, caseId, resultId);

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-runs/{runId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TEST_RUN_HAS_DEFECTS", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task DeleteTestRun_ThatHasADefect_KeepsTheRun()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (runId, caseId, resultId) = await NewRunWithOneResultAsync(alice, projectId, "Fail");
        await RaiseDefectAsync(alice, projectId, caseId, resultId);

        await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-runs/{runId}");

        var read = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    // Passes before and after on purpose. It proves the fix does not block a run that has results but no defects.
    [Fact]
    public async Task DeleteTestRun_WithResultsButNoDefects_StillWorks()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (runId, _, _) = await NewRunWithOneResultAsync(alice, projectId, "Fail");

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-runs/{runId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}