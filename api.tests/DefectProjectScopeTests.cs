using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class DefectProjectScopeTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public DefectProjectScopeTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    // A project owned by the given user, with one failed result and one defect raised from it.
    private async Task<(Guid ProjectId, Guid DefectId)> NewDefectAsync(TestUser owner)
    {
        var projectId = await ApiTestHelpers.CreateProjectAsync(owner);
        var (caseId, resultId) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var created = await owner.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId = caseId, testRunResultId = resultId, title = "Login button does nothing", description = "", severity = "High" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var defectId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (projectId, defectId);
    }

    [Fact]
    public async Task UpdateDefect_UsingAnotherProjectsDefect_ReturnsNotFound()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (_, aliceDefect) = await NewDefectAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        // Bob puts his own project id in the URL, with Alice's defect id.
        var response = await bob.Client.PutAsJsonAsync($"/api/v1/projects/{bobProject}/defects/{aliceDefect}", new { status = "Closed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDefect_TamperingAttempt_DoesNotChangeTheDefect()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (aliceProject, aliceDefect) = await NewDefectAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        await bob.Client.PutAsJsonAsync($"/api/v1/projects/{bobProject}/defects/{aliceDefect}", new { status = "Closed" });

        var read = await alice.Client.GetAsync($"/api/v1/projects/{aliceProject}/defects/{aliceDefect}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var body = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Open", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeleteDefect_UsingAnotherProjectsDefect_ReturnsNotFound()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (_, aliceDefect) = await NewDefectAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        var response = await bob.Client.DeleteAsync($"/api/v1/projects/{bobProject}/defects/{aliceDefect}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDefect_TamperingAttempt_DoesNotRemoveTheDefect()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var (aliceProject, aliceDefect) = await NewDefectAsync(alice);
        var bobProject = await ApiTestHelpers.CreateProjectAsync(bob);

        await bob.Client.DeleteAsync($"/api/v1/projects/{bobProject}/defects/{aliceDefect}");

        var read = await alice.Client.GetAsync($"/api/v1/projects/{aliceProject}/defects/{aliceDefect}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    // The two tests below pass before and after on purpose. They prove the scope check does not lock out the legitimate owner.
    [Fact]
    public async Task UpdateDefect_ByTheOwnerInTheirOwnProject_StillWorks()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var (projectId, defectId) = await NewDefectAsync(alice);

        var response = await alice.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}/defects/{defectId}", new { status = "In Progress" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDefect_ByTheOwnerInTheirOwnProject_StillWorks()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var (projectId, defectId) = await NewDefectAsync(alice);

        var response = await alice.Client.DeleteAsync($"/api/v1/projects/{projectId}/defects/{defectId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}