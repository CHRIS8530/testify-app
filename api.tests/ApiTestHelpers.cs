using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public record TestUser(HttpClient Client, Guid Id, string Email);

public static class ApiTestHelpers
{
    private const string Password = "Passw0rd!test";

    // Registers and signs in a brand new user. Every call uses a unique email, because tests share one in-memory database.
    public static async Task<TestUser> CreateUserAsync(TestifyApiFactory factory)
    {
        var unique = Guid.NewGuid().ToString("N")[..12];
        var email = $"user{unique}@example.com";
        var username = $"user{unique}";
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, username, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString()!;
        var userId = body.GetProperty("user").GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new TestUser(client, userId, email);
    }

    public static async Task<Guid> CreateProjectAsync(TestUser owner)
    {
        var response = await owner.Client.PostAsJsonAsync("/api/v1/projects", new { name = $"Project {Guid.NewGuid():N}", description = "created by a test" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    public static async Task InviteAsync(TestUser owner, Guid projectId, TestUser invitee, string role)
    {
        var response = await owner.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/members/invite", new { email = invitee.Email, role });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public static Task<HttpResponseMessage> PostTestCaseAsync(TestUser user, Guid projectId)
    {
        return user.Client.PostAsJsonAsync($"/api/v1/projects/{projectId}/test-cases", new { title = "Login with valid credentials", preconditions = "", steps = "1. Open the login page", expectedResult = "The user is signed in", priority = "High" });
    }
}
