using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Testify.Api;
using Testify.Api.Data;
using Testify.Api.Models;
using Xunit;

namespace Testify.Api.Tests
{
    public class ProjectsControllerTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;

        public async Task InitializeAsync()
        {
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");
                });

            _client = factory.CreateClient();
            _factory = factory;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();
                db.Database.EnsureCreated();
            }
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [Fact]
        public async Task GetProjects_ReturnsOkWithEmptyList()
        {
            var response = await _client!.GetAsync("/api/v1/projects");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            Assert.Equal(200, json.RootElement.GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task CreateProject_WithValidData_ReturnsCreated()
        {
            var userId = Guid.NewGuid();
            var payload = new
            {
                name = "Test Project",
                description = "A test project",
                ownerId = userId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/api/v1/projects", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithoutName_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            var payload = new
            {
                name = "",
                description = "A test project",
                ownerId = userId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/api/v1/projects", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetProject_WithNonexistentId_ReturnsNotFound()
        {
            var fakeId = Guid.NewGuid();
            var response = await _client!.GetAsync($"/api/v1/projects/{fakeId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}