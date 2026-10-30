using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class DefectListTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public DefectListTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static Task<HttpResponseMessage> PostDefectAsync(TestUser user, Guid projectId, Guid caseId, Guid resultId, string title)
    {
        return user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/defects", new { testCaseId = caseId, testRunResultId = resultId, title, description = "", severity = "High" });
    }

    [Fact]
    public async Task GetDefects_ListsTheNewestDefectFirst()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        var (caseA, resultA) = await SeedData.AddResultAsync(_factory, projectId, "Fail");
        var (caseB, resultB) = await SeedData.AddResultAsync(_factory, projectId, "Fail");

        var first = await PostDefectAsync(alice, projectId, caseA, resultA, "First defect");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await Task.Delay(50);
        var second = await PostDefectAsync(alice, projectId, caseB, resultB, "Second defect");
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/defects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var defects = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(2, defects.Count);
        Assert.Equal("Second defect", defects[0].GetProperty("title").GetString());
    }
}