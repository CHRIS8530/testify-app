using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId}/test-runs")]
public class TestRunsController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TestRunsController> _logger;

    public TestRunsController(TestifyDbContext db, IAuditLogService auditLogService, ILogger<TestRunsController> logger)
    {
        _db = db;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    // One row per run with the number of results in each outcome, counted by the database.
    private IQueryable<RunCounts> RunsWithCounts(Guid projectId)
    {
        return _db.TestRuns
            .Where(tr => tr.ProjectId == projectId)
            .Select(tr => new RunCounts
            {
                Id = tr.Id,
                ProjectId = tr.ProjectId,
                Status = tr.Status,
                CreatedAt = tr.CreatedAt,
                UpdatedAt = tr.UpdatedAt,
                CompletedAt = tr.CompletedAt,
                Total = tr.Results.Count,
                Passed = tr.Results.Count(r => r.Result == "Pass"),
                Failed = tr.Results.Count(r => r.Result == "Fail"),
                Blocked = tr.Results.Count(r => r.Result == "Blocked"),
                NotRun = tr.Results.Count(r => r.Result == "Not Run")
            });
    }

    // The pass rate is worked out in one place so every screen shows the same number.
    private static object ToSummary(RunCounts run)
    {
        return new
        {
            run.Id,
            run.ProjectId,
            run.Status,
            run.CreatedAt,
            run.UpdatedAt,
            run.CompletedAt,
            run.Total,
            run.Passed,
            run.Failed,
            run.Blocked,
            run.NotRun,
            PassRate = RunStats.PassRate(run.Passed, run.Total)
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetTestRuns(Guid projectId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var (denied, _) = await this.RequireProjectAccessAsync(_db, projectId, ProjectRole.Viewer);
        if (denied is not null) return denied;

        var runs = await RunsWithCounts(projectId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return Ok(runs.Select(ToSummary));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestRun(Guid projectId, Guid id)
    {
        var (denied, _) = await this.RequireProjectAccessAsync(_db, projectId, ProjectRole.Viewer);
        if (denied is not null) return denied;

        var run = await RunsWithCounts(projectId).FirstOrDefaultAsync(r => r.Id == id);
        if (run == null)
            return NotFound();

        return Ok(ToSummary(run));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTestRun(Guid projectId)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        if (!await _db.Projects.AnyAsync(p => p.Id == projectId))
            return NotFound();

        if (await ProjectAccess.GetRoleAsync(_db, projectId, userId) < ProjectRole.Editor)
            return this.ForbiddenResponse();

        var testRun = new TestRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Status = "In Progress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TestRuns.Add(testRun);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "create_test_run",
            "test_run",
            testRun.Id,
            projectId,
            new Dictionary<string, object> { { "status", testRun.Status } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetTestRun), new { projectId, id = testRun.Id }, testRun);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTestRun(Guid projectId, Guid id, [FromBody] UpdateTestRunRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var testRun = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == id && tr.ProjectId == projectId);
        if (testRun == null)
            return NotFound();

        if (await ProjectAccess.GetRoleAsync(_db, projectId, userId) < ProjectRole.Editor)
            return this.ForbiddenResponse();

        if (!Allowed.RunStatuses.Contains(req.Status))
            return BadRequest(new { error = new { message = "Status must be one of: In Progress, Complete", code = "INVALID_STATUS" }, status = 400 });

        testRun.Status = req.Status;
        testRun.CompletedAt = req.Status == "Complete" ? (DateTime?)(testRun.CompletedAt ?? DateTime.UtcNow) : null;
        testRun.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "update_test_run",
            "test_run",
            testRun.Id,
            projectId,
            new Dictionary<string, object> { { "status", testRun.Status } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(testRun);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTestRun(Guid projectId, Guid id)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var testRun = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == id && tr.ProjectId == projectId);
        if (testRun == null)
            return NotFound();

        if (await ProjectAccess.GetRoleAsync(_db, projectId, userId) < ProjectRole.Editor)
            return this.ForbiddenResponse();

        _db.TestRuns.Remove(testRun);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "delete_test_run",
            "test_run",
            testRun.Id,
            projectId,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }

    private class RunCounts
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Blocked { get; set; }
        public int NotRun { get; set; }
    }
}

public class UpdateTestRunRequest
{
    public string Status { get; set; } = string.Empty;
}