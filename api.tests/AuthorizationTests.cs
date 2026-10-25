using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class AuthorizationTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public AuthorizationTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<List<Guid>> ListProjectIdsAsync(TestUser user)
    {
        var response = await user.Client.GetAsync("/api/v1/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.EnumerateArray().Select(project => project.GetProperty("id").GetGuid()).ToList();
    }

    [Fact]
    public async Task GetProjects_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_WithoutToken_ReturnsUnauthorized()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await _factory.CreateClient().GetAsync($"/api/v1/projects/{projectId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyProjectsTheUserBelongsTo()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var carol = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await ApiTestHelpers.InviteAsync(alice, projectId, carol, "viewer");

        Assert.Contains(projectId, await ListProjectIdsAsync(alice));
        Assert.Contains(projectId, await ListProjectIdsAsync(carol));
        Assert.DoesNotContain(projectId, await ListProjectIdsAsync(bob));
    }

    [Fact]
    public async Task GetProject_AsNonMember_ReturnsForbidden()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await bob.Client.GetAsync($"/api/v1/projects/{projectId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_AsInvitedViewer_ReturnsOk()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await ApiTestHelpers.InviteAsync(alice, projectId, bob, "viewer");

        var response = await bob.Client.GetAsync($"/api/v1/projects/{projectId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("test-cases")]
    [InlineData("test-runs")]
    [InlineData("defects")]
    [InlineData("dashboard")]
    public async Task NestedReads_WithoutToken_ReturnUnauthorized(string segment)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await _factory.CreateClient().GetAsync($"/api/v1/projects/{projectId}/{segment}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("test-cases")]
    [InlineData("test-runs")]
    [InlineData("defects")]
    [InlineData("dashboard")]
    public async Task NestedReads_AsNonMember_ReturnForbidden(string segment)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await bob.Client.GetAsync($"/api/v1/projects/{projectId}/{segment}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("test-cases")]
    [InlineData("test-runs")]
    [InlineData("defects")]
    [InlineData("dashboard")]
    public async Task NestedReads_AsOwner_ReturnOk(string segment)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync($"/api/v1/projects/{projectId}/{segment}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_AsNonMember_ReturnsForbidden()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await bob.Client.PutAsJsonAsync($"/api/v1/projects/{projectId}", new { name = "Renamed by a stranger", description = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestCase_AsNonMember_ReturnsForbidden()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await ApiTestHelpers.PostTestCaseAsync(bob, projectId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestCase_AsViewer_ReturnsForbidden()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await ApiTestHelpers.InviteAsync(alice, projectId, bob, "viewer");

        var response = await ApiTestHelpers.PostTestCaseAsync(bob, projectId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestCase_AsOwner_ReturnsCreated()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await ApiTestHelpers.PostTestCaseAsync(alice, projectId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestCase_AsEditor_ReturnsCreated()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await ApiTestHelpers.InviteAsync(alice, projectId, bob, "editor");

        var response = await ApiTestHelpers.PostTestCaseAsync(bob, projectId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
