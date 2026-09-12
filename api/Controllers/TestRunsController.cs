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

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        var testRun = new TestRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Status = "pending",
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

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        testRun.Status = req.Status;
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

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

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