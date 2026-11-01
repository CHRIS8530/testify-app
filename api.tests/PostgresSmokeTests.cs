using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class PostgresSmokeTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PostgresSmokeTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task OnARealPostgres_AUserCanRegisterSignInAndCreateAProject()
    {
        Skip.IfNot(_factory.DockerAvailable, "Docker is not available on this machine. This test runs in CI.");

        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var projectId = await ApiTestHelpers.CreateProjectAsync(alice);

        var response = await alice.Client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Contains(projects, project => project.GetProperty("id").GetGuid() == projectId);
    }
}