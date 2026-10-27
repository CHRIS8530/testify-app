using Microsoft.Extensions.DependencyInjection;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Tests;

public static class SeedData
{
    // Puts a test case, a run and one result with the given outcome straight into the database,
    // so the defect tests do not depend on the endpoints that create them.
    public static async Task<(Guid CaseId, Guid ResultId)> AddResultAsync(TestifyApiFactory factory, Guid projectId, string outcome)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestifyDbContext>();
        var now = DateTime.UtcNow;

        var testCase = new TestCase { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Seeded test case", Preconditions = "", Steps = "1. Do something", ExpectedResult = "It works", Priority = "High", CreatedAt = now, UpdatedAt = now };
        var run = new TestRun { Id = Guid.NewGuid(), ProjectId = projectId, Status = "In Progress", CreatedAt = now, UpdatedAt = now };
        var result = new TestRunResult { Id = Guid.NewGuid(), TestRunId = run.Id, TestCaseId = testCase.Id, Result = outcome, CreatedAt = now, UpdatedAt = now };

        db.AddRange(testCase, run, result);
        await db.SaveChangesAsync();

        return (testCase.Id, result.Id);
    }
}
