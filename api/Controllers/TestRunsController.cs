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

    [HttpGet]
    public async Task<IActionResult> GetTestRuns(Guid projectId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var (denied, _) = await this.RequireProjectAccessAsync(_db, projectId, ProjectRole.Viewer);
        if (denied is not null) return denied;
        var testRuns = await _db.TestRuns
            .Where(tr => tr.ProjectId == projectId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return Ok(testRuns);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestRun(Guid projectId, Guid id)
    {
        var (denied, _) = await this.RequireProjectAccessAsync(_db, projectId, ProjectRole.Viewer);
        if (denied is not null) return denied;
        var testRun = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == id && tr.ProjectId == projectId);
        if (testRun == null)
            return NotFound();

        return Ok(testRun);
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
}

public class UpdateTestRunRequest
{
    public string Status { get; set; } = string.Empty;
}