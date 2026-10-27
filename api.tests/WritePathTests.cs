using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class WritePathTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public WritePathTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static object TestCaseBody(string priority)
    {
        return new { title = "Login with valid credentials", preconditions = "", steps = "1. Open the login page", expectedResult = "The user is signed in", priority };
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid> CreateTestCaseAsync(TestUser user, Guid projectId)
    {
        var response = await ApiTestHelpers.PostTestCaseAsync(user, projectId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateRunAsync(TestUser user, Guid projectId)
    {
        var response = await user.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> PostDefectAsync(TestUser user, Guid projectId, Guid testCaseId, Guid resultId, string severity = "High")
    {
        return user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId, testRunResultId = resultId, title = "Login button does nothing", description = "Clicking it has no effect", severity });
    }

    [Fact]
    public async Task CreateTestCase_WithAnInvalidPriority_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-cases", TestCaseBody("Urgent"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTestCase_WithAnInvalidPriority_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var caseId = await CreateTestCaseAsync(alice, projectId);

        var response = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/test-cases/{caseId}", TestCaseBody("Urgent"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestRun_StartsInProgress()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("In Progress", (await ReadAsync(response)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdateTestRun_WithAnInvalidStatus_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var runId = await CreateRunAsync(alice, projectId);

        var response = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}", new { status = "Banana" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTestRun_ToComplete_SetsCompletedAt()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var runId = await CreateRunAsync(alice, projectId);

        var response = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}", new { status = "Complete" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal("Complete", body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task AddCasesToRun_ReturnsTheCreatedResults()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var caseId = await CreateTestCaseAsync(alice, projectId);
        var runId = await CreateRunAsync(alice, projectId);

        var response = await alice.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{runId}/results", new { testCaseIds = new[] { caseId } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var results = (await ReadAsync(response)).EnumerateArray().ToList();
        var single = Assert.Single(results);
        Assert.Equal(caseId, single.GetProperty("testCaseId").GetGuid());
        Assert.Equal("Not Run", single.GetProperty("result").GetString());
    }

    [Fact]
    public async Task CreateDefect_StartsOpenAndIsCountedByTheDashboard()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var created = await PostDefectAsync(alice, projectId, caseId, resultId, "Critical");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Open", (await ReadAsync(created)).GetProperty("status").GetString());

        var dashboard = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/dashboard");
        Assert.Equal(1, (await ReadAsync(dashboard)).GetProperty("open_defects").GetInt32());
    }

    [Fact]
    public async Task CreateDefect_WithAnInvalidSeverity_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var response = await PostDefectAsync(alice, projectId, caseId, resultId, "Urgent");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDefect_WithAResultFromAnotherProject_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var aliceProject = await ApiTestHelpers.CreateProjectAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, aliceProject, "Fail");

        var response = await PostDefectAsync(bob, bobProject, caseId, resultId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDefect_FromAResultThatDidNotFail_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Pass");

        var response = await PostDefectAsync(alice, projectId, caseId, resultId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDefect_WithoutAResult_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, _) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var response = await PostDefectAsync(alice, projectId, caseId, Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDefect_WithAnInvalidStatus_ReturnsBadRequest()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");
        var created = await PostDefectAsync(alice, projectId, caseId, resultId);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var defectId = (await ReadAsync(created)).GetProperty("id").GetGuid();

        var response = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/defects/{defectId}", new { status = "Banana" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
