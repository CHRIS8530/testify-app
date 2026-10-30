using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class DashboardTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public DashboardTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> GetDashboardAsync(TestUser user, Guid projectId)
    {
        var response = await user.Client.GetAsync($"/api/v1/projects/{projectId}/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid> CreateRunAsync(TestUser user, Guid projectId)
    {
        var response = await user.Client.PostAsync($"/api/v1/projects/{projectId}/test-runs", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Dashboard_CountsCompletedRuns()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var first = await CreateRunAsync(alice, projectId);
        await CreateRunAsync(alice, projectId);
        var complete = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/test-runs/{first}", new { status = "Complete" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var body = await GetDashboardAsync(alice, projectId);

        Assert.Equal(2, body.GetProperty("total_runs").GetInt32());
        Assert.Equal(1, body.GetProperty("runs_completed").GetInt32());
    }

    [Fact]
    public async Task Dashboard_PassRateRoundsHalvesUpLikeTheRunList()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await SeedData.AddResultAsync(_factory, projectId, "Pass");
        for (var i = 0; i < 7; i++)
            await SeedData.AddResultAsync(_factory, projectId, "Not Run");

        var body = await GetDashboardAsync(alice, projectId);

        Assert.Equal(8, body.GetProperty("total_results").GetInt32());
        Assert.Equal(13, body.GetProperty("pass_rate").GetInt32());
    }

    [Fact]
    public async Task Dashboard_CurrentPassRateIsTheLatestRun()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await SeedData.AddResultAsync(_factory, projectId, "Fail");
        await Task.Delay(50);
        await SeedData.AddResultAsync(_factory, projectId, "Pass");

        var body = await GetDashboardAsync(alice, projectId);

        var latest = body.GetProperty("latest_run");
        Assert.Equal(100, latest.GetProperty("pass_rate").GetInt32());
        Assert.Equal(1, latest.GetProperty("total").GetInt32());
        Assert.Equal(50, body.GetProperty("pass_rate").GetInt32());
    }

    [Fact]
    public async Task Dashboard_WithNoRuns_HasNoLatestRunAndZeros()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var body = await GetDashboardAsync(alice, projectId);

        Assert.Equal(JsonValueKind.Null, body.GetProperty("latest_run").ValueKind);
        Assert.Equal(0, body.GetProperty("pass_rate").GetInt32());
        Assert.Equal(0, body.GetProperty("runs_completed").GetInt32());
        Assert.Equal(0, body.GetProperty("total_runs").GetInt32());
    }

    [Fact]
    public async Task Dashboard_ListsOpenDefectsBySeverity()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");
        var created = await alice.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId = caseId, testRunResultId = resultId, title = "Checkout crashes", description = "", severity = "Critical" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var body = await GetDashboardAsync(alice, projectId);

        var severities = body.GetProperty("defects_by_severity").EnumerateArray().ToList();
        var critical = Assert.Single(severities);
        Assert.Equal("Critical", critical.GetProperty("severity").GetString());
        Assert.Equal(1, critical.GetProperty("count").GetInt32());
    }
}