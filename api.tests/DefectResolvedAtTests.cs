using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class DefectResolvedAtTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public DefectResolvedAtTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(TestUser User, Guid ProjectId, Guid DefectId)> NewDefectAsync()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var created = await alice.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId = caseId, testRunResultId = resultId, title = "Login button does nothing", description = "", severity = "High" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var defectId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (alice, projectId, defectId);
    }

    private static async Task<JsonElement> SetStatusAsync(TestUser user, Guid projectId, Guid defectId, string status)
    {
        var response = await user.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/defects/{defectId}", new { status });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // Passes before and after on purpose. A defect that is being worked on is not resolved yet.
    [Fact]
    public async Task InProgress_HasNoResolvedAt()
    {
        var (alice, projectId, defectId) = await NewDefectAsync();

        var body = await SetStatusAsync(alice, projectId, defectId, "In Progress");

        Assert.Equal(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
    }

    [Fact]
    public async Task Resolved_SetsResolvedAt()
    {
        var (alice, projectId, defectId) = await NewDefectAsync();

        var body = await SetStatusAsync(alice, projectId, defectId, "Resolved");

        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
    }

    [Fact]
    public async Task Closed_SetsResolvedAt()
    {
        var (alice, projectId, defectId) = await NewDefectAsync();

        var body = await SetStatusAsync(alice, projectId, defectId, "Closed");

        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
    }

    [Fact]
    public async Task Reopening_ClearsResolvedAt()
    {
        var (alice, projectId, defectId) = await NewDefectAsync();
        var resolved = await SetStatusAsync(alice, projectId, defectId, "Resolved");
        Assert.NotEqual(JsonValueKind.Null, resolved.GetProperty("resolvedAt").ValueKind);

        var reopened = await SetStatusAsync(alice, projectId, defectId, "Open");

        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("resolvedAt").ValueKind);
    }
}