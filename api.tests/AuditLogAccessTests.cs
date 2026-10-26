using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class AuditLogAccessTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public AuditLogAccessTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> GetLogsAsync(TestUser user, string query = "")
    {
        var response = await user.Client.GetAsync($"/api/v1/admin/logs{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static List<JsonElement> Entries(JsonElement body)
    {
        return body.GetProperty("logs").EnumerateArray().ToList();
    }

    private static bool IsBy(JsonElement entry, Guid userId)
    {
        var value = entry.GetProperty("userId");
        return value.ValueKind == JsonValueKind.String && value.GetGuid() == userId;
    }

    private static bool IsOnProject(JsonElement entry, Guid projectId)
    {
        var value = entry.GetProperty("projectId");
        return value.ValueKind == JsonValueKind.String && value.GetGuid() == projectId;
    }

    [Fact]
    public async Task GetAuditLogs_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/admin/logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsTheUsersOwnEntries()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        await ApiTestHelpers.CreateProjectAsync(alice);

        var entries = Entries(await GetLogsAsync(alice));

        Assert.Contains(entries, entry => IsBy(entry, alice.Id));
    }

    [Fact]
    public async Task GetAuditLogs_DoesNotReturnOtherUsersEntries()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var entries = Entries(await GetLogsAsync(bob));

        Assert.DoesNotContain(entries, entry => IsBy(entry, alice.Id));
        Assert.DoesNotContain(entries, entry => IsOnProject(entry, projectId));
    }

    [Fact]
    public async Task GetAuditLogs_ProjectOwnerSeesOthersActionsOnTheirProject()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);
        await ApiTestHelpers.InviteAsync(alice, projectId, bob, "editor");
        var created = await ApiTestHelpers.PostTestCaseAsync(bob, projectId);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var entries = Entries(await GetLogsAsync(alice));

        Assert.Contains(entries, entry => IsBy(entry, bob.Id) && IsOnProject(entry, projectId));
    }

    [Fact]
    public async Task GetAuditLogs_FilteredToSomeoneElsesProject_ReturnsNothing()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var body = await GetLogsAsync(bob, $"?projectId={projectId}");

        Assert.Equal(0, body.GetProperty("total").GetInt32());
    }
}
