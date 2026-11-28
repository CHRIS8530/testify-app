using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class TestCaseDeleteTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public TestCaseDeleteTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> CreateCaseAsync(TestUser user, Guid projectId)
    {
        var response = await ApiTestHelpers.PostTestCaseAsync(user, projectId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // A run that contains the given case, so the case now has a result.
    private static async Task PutCaseInARunAsync(TestUser user, Guid projectId, Guid caseId)
    {
        var run = await user.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, run.StatusCode);
        var runId = (await run.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var added = await user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = new[] { caseId } });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
    }

    [Fact]
    public async Task DeleteTestCase_ThatIsInARun_ReturnsConflict()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var caseId = await CreateCaseAsync(alice, projectId);
        await PutCaseInARunAsync(alice, projectId, caseId);

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TEST_CASE_IN_USE", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task DeleteTestCase_ThatIsInARun_KeepsTheTestCase()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var caseId = await CreateCaseAsync(alice, projectId);
        await PutCaseInARunAsync(alice, projectId, caseId);

        await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}");

        var read = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    // Passes before and after on purpose. It proves the fix does not block a case that is in no run.
    [Fact]
    public async Task DeleteTestCase_ThatIsInNoRun_StillWorks()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var caseId = await CreateCaseAsync(alice, projectId);

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}