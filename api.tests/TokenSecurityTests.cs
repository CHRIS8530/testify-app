using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testify.Api.Data;
using Xunit;

namespace Testify.Api.Tests;

public class TokenSecurityTests : IClassFixture<TestifyApiFactory>
{
    private const string CookieName = "refreshToken";
    private readonly TestifyApiFactory _factory;

    public TokenSecurityTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    private record Session(HttpClient Client, string Email, Guid UserId, string RefreshToken, string SetCookie);

    // The refresh cookie is Secure, so a test client on plain http would not send it back by itself.
    // Cookie handling is switched off and the cookie is sent by hand instead.
    private HttpClient NewClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private static string? SetCookieHeader(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;
    }

    private static string? CookieValue(HttpResponseMessage response)
    {
        var header = SetCookieHeader(response);
        return header?.Split(';')[0][(CookieName.Length + 1)..];
    }

    private static Task<HttpResponseMessage> PostWithCookieAsync(HttpClient client, string path, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (refreshToken is not null)
            request.Headers.Add("Cookie", $"{CookieName}={refreshToken}");
        return client.SendAsync(request);
    }

    private async Task<Session> SignInAsync()
    {
        var client = NewClient();
        var unique = Guid.NewGuid().ToString("N")[..12];
        var email = $"user{unique}@example.com";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, username = $"user{unique}", password = ApiTestHelpers.Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = ApiTestHelpers.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var userId = body.GetProperty("user").GetProperty("id").GetGuid();
        var setCookie = SetCookieHeader(login);
        var token = CookieValue(login);
        Assert.NotNull(setCookie);
        Assert.NotNull(token);

        return new Session(client, email, userId, token, setCookie);
    }

    private async Task<(Session Account, string ResetToken)> RequestPasswordResetAsync()
    {
        var account = await SignInAsync();
        var emails = _factory.Services.GetRequiredService<CapturingEmailService>();

        var forgot = await account.Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = account.Email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        return (account, emails.ResetTokens[account.Email]);
    }

    [Fact]
    public async Task Login_SetsAnHttpOnlySecureStrictRefreshCookie()
    {
        var session = await SignInAsync();

        Assert.Contains("httponly", session.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", session.SetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", session.SetCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_RefreshCookieIsOnlySentToTheAuthEndpoints()
    {
        var session = await SignInAsync();

        Assert.Contains("path=/api/v1/auth", session.SetCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_WithTheCookie_ReturnsAnAccessTokenAndTheUser()
    {
        var session = await SignInAsync();

        var response = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", session.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal(session.Email, body.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Refresh_IssuesANewRefreshTokenEachTime()
    {
        var session = await SignInAsync();

        var first = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", session.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = CookieValue(first);
        Assert.NotNull(rotated);
        Assert.NotEqual(session.RefreshToken, rotated);

        var second = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", rotated);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithAnAlreadyUsedToken_ReturnsUnauthorized()
    {
        var session = await SignInAsync();
        var first = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", session.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", session.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutTheCookie_ReturnsUnauthorized()
    {
        var response = await PostWithCookieAsync(NewClient(), "/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var session = await SignInAsync();

        var logout = await PostWithCookieAsync(session.Client, "/api/v1/auth/logout", session.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await PostWithCookieAsync(session.Client, "/api/v1/auth/refresh", session.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RefreshTokens_AreStoredHashed()
    {
        var session = await SignInAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();

        Assert.True(await db.RefreshTokens.AnyAsync(token => token.UserId == session.UserId));
        Assert.False(await db.RefreshTokens.AnyAsync(token => token.Token == session.RefreshToken));
    }

    [Fact]
    public async Task PasswordReset_WorksOnce()
    {
        var (account, resetToken) = await RequestPasswordResetAsync();

        var first = await account.Client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = resetToken, newPassword = "N3w!Passw0rd" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await account.Client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = resetToken, newPassword = "An0ther!Passw0rd" });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        var login = await account.Client.PostAsJsonAsync("/api/v1/auth/login", new { email = account.Email, password = "N3w!Passw0rd" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task PasswordResetTokens_AreStoredHashed()
    {
        var (account, resetToken) = await RequestPasswordResetAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();

        Assert.True(await db.PasswordResets.AnyAsync(reset => reset.UserId == account.UserId));
        Assert.False(await db.PasswordResets.AnyAsync(reset => reset.Token == resetToken));
    }
}
