using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Testify.Api.Tests;

public class LoginThrottleTests : IClassFixture<TestifyApiFactory>
{
    private const int MaxFailures = 5;
    private const string WrongPassword = "Wrong!Passw0rd1";
    private readonly TestifyApiFactory _factory;

    public LoginThrottleTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        return client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
    }

    private static async Task FailAsync(HttpClient client, string email, int times)
    {
        for (var i = 0; i < times; i++)
        {
            var response = await LoginAsync(client, email, WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Login_AfterTooManyFailures_ReturnsTooManyRequestsEvenWithTheRightPassword()
    {
        var user = await ApiTestHelpers.CreateUserAsync(_factory);
        var client = _factory.CreateClient();
        await FailAsync(client, user.Email, MaxFailures);

        var blocked = await LoginAsync(client, user.Email, ApiTestHelpers.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TOO_MANY_ATTEMPTS", body.GetProperty("error").GetProperty("code").GetString());
    }

    // Passes before and after the change on purpose. It proves a successful sign in clears the count.
    [Fact]
    public async Task Login_WithTheRightPassword_ClearsTheFailureCount()
    {
        var user = await ApiTestHelpers.CreateUserAsync(_factory);
        var client = _factory.CreateClient();
        await FailAsync(client, user.Email, MaxFailures - 1);

        var good = await LoginAsync(client, user.Email, ApiTestHelpers.Password);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);

        await FailAsync(client, user.Email, MaxFailures - 1);
        var again = await LoginAsync(client, user.Email, ApiTestHelpers.Password);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Login_ThrottleOnlyAffectsTheEmailThatFailed()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);
        var bob = await ApiTestHelpers.CreateUserAsync(_factory);
        var client = _factory.CreateClient();
        await FailAsync(client, alice.Email, MaxFailures);

        var blocked = await LoginAsync(client, alice.Email, ApiTestHelpers.Password);
        var allowed = await LoginAsync(client, bob.Email, ApiTestHelpers.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Login_ForAnUnknownEmail_IsThrottledToo()
    {
        var client = _factory.CreateClient();
        var email = $"nobody{Guid.NewGuid():N}@example.com";
        await FailAsync(client, email, MaxFailures);

        var blocked = await LoginAsync(client, email, WrongPassword);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }
}