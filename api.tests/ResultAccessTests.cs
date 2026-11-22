using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class ResultAccessTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public ResultAccessTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    // A project with one test case, one run and one result in it.
    private static async Task<(Guid ProjectId, Guid RunId, Guid ResultId)> NewResultAsync(TestUser owner)
    {
        var projectId = await ApiTestHelpers.CreateProjectAsync(owner);

        var caseResponse = await ApiTestHelpers.PostTestCaseAsync(owner, projectId);
        Assert.Equal(HttpStatusCode.Created, caseResponse.StatusCode);
        var caseId = (await caseResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var runResponse = await owner.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, runResponse.StatusCode);
        var runId = (await runResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var added = await owner.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = new[] { caseId } });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var resultId = (await added.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().First().GetProperty("id").GetGuid();

        return (projectId, runId, resultId);
    }

    [Fact]
    public async Task UpdateResult_UsingAnotherProjectsRunAndResult_IsRejected()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (_, aliceRun, aliceResult) = await NewResultAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        // Bob puts his own project id in the URL, with Alice's run and result ids.
        var response = await bob.Client.PatchAsJsonAsync($"/api/v1/projects/{bobProject}/test-runs/{aliceRun}/results/{aliceResult}", new { result = "Pass", notes = "tampered" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_TamperingAttempt_DoesNotChangeTheResult()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (aliceProject, aliceRun, aliceResult) = await NewResultAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        await bob.Client.PatchAsJsonAsync($"/api/v1/projects/{bobProject}/test-runs/{aliceRun}/results/{aliceResult}", new { result = "Pass", notes = "tampered" });

        var list = await alice.Client.GetAsync($"/api/v1/projects/{aliceProject}/test-runs/{aliceRun}/results");
        var row = (await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
        Assert.Equal("Not Run", row.GetProperty("result").GetString());
    }

    // Passes before and after on purpose. It proves the fix does not lock out the legitimate case.
    [Fact]
    public async Task UpdateResult_ByTheOwnerInTheirOwnProject_StillWorks()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var (projectId, runId, resultId) = await NewResultAsync(alice);

        var response = await alice.Client.PatchAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results/{resultId}", new { result = "Pass", notes = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}