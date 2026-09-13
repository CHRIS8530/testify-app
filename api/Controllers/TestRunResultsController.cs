using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId}/test-runs/{runId}/results")]
public class TestRunResultsController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TestRunResultsController> _logger;

    public TestRunResultsController(TestifyDbContext db, IAuditLogService auditLogService, ILogger<TestRunResultsController> logger)
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
    public async Task<IActionResult> GetResults(Guid projectId, Guid runId)
    {
        var run = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == runId && tr.ProjectId == projectId);
        if (run == null)
            return NotFound(new { error = new { message = "Test run not found", code = "NOT_FOUND" }, status = 404 });

        var results = await _db.TestRunResults
            .Where(r => r.TestRunId == runId)
            .Include(r => r.TestCase)
            .Select(r => new
            {
                r.Id,
                r.TestRunId,
                r.TestCaseId,
                TestCaseTitle = r.TestCase.Title,
                r.Result,
                r.Notes,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> AddCasesToRun(Guid projectId, Guid runId, [FromBody] AddCasesToRunRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var run = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == runId && tr.ProjectId == projectId);
        if (run == null)
            return NotFound(new { error = new { message = "Test run not found", code = "NOT_FOUND" }, status = 404 });

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        if (req.TestCaseIds == null || req.TestCaseIds.Count == 0)
            return BadRequest(new { error = new { message = "At least one test case ID is required", code = "INVALID_INPUT" }, status = 400 });

        var validCaseIds = await _db.TestCases
            .Where(tc => tc.ProjectId == projectId && req.TestCaseIds.Contains(tc.Id))
            .Select(tc => tc.Id)
            .ToListAsync();

        var newResults = new List<TestRunResult>();
        foreach (var caseId in validCaseIds)
        {
            var exists = await _db.TestRunResults.AnyAsync(r => r.TestRunId == runId && r.TestCaseId == caseId);
            if (!exists)
            {
                newResults.Add(new TestRunResult
                {
                    Id = Guid.NewGuid(),
                    TestRunId = runId,
                    TestCaseId = caseId,
                    Result = "Not Run",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        _db.TestRunResults.AddRange(newResults);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "add_cases_to_run",
            "test_run",
            runId,
            projectId,
            new Dictionary<string, object> { { "case_count", newResults.Count } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetResults), new { projectId, runId }, newResults);
    }

    [HttpPatch("{resultId}")]
    public async Task<IActionResult> UpdateResult(Guid projectId, Guid runId, Guid resultId, [FromBody] UpdateResultRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var result = await _db.TestRunResults.FirstOrDefaultAsync(r => r.Id == resultId && r.TestRunId == runId);
        if (result == null)
            return NotFound(new { error = new { message = "Result not found", code = "NOT_FOUND" }, status = 404 });

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        var validResults = new[] { "Not Run", "Pass", "Fail", "Blocked" };
        if (string.IsNullOrWhiteSpace(req.Result) || !validResults.Contains(req.Result))
            return BadRequest(new { error = new { message = "Result must be one of: Not Run, Pass, Fail, Blocked", code = "INVALID_RESULT" }, status = 400 });

        result.Result = req.Result;
        result.Notes = req.Notes;
        result.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "update_test_result",
            "test_run_result",
            result.Id,
            projectId,
            new Dictionary<string, object> { { "result", result.Result } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(result);
    }
}

public class AddCasesToRunRequest
{
    public List<Guid> TestCaseIds { get; set; } = new();
}

public class UpdateResultRequest
{
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
}