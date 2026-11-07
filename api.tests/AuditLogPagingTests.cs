using System.Net;
using Xunit;

namespace Testify.Api.Tests;

public class AuditLogPagingTests : IClassFixture<TestifyApiFactory>
{
    private readonly TestifyApiFactory _factory;

    public AuditLogPagingTests(TestifyApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("offset=-5")]
    [InlineData("limit=-1")]
    [InlineData("limit=0")]
    [InlineData("limit=201")]
    public async Task GetAuditLogs_WithAnInvalidPagingValue_ReturnsBadRequest(string query)
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await alice.Client.GetAsync($"/api/v1/admin/logs?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Passes before and after the change on purpose. It proves the fix does not reject the largest allowed page.
    [Fact]
    public async Task GetAuditLogs_WithTheMaximumLimit_ReturnsOk()
    {
        var alice = await ApiTestHelpers.CreateUserAsync(_factory);

        var response = await alice.Client.GetAsync("/api/v1/admin/logs?limit=200&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}