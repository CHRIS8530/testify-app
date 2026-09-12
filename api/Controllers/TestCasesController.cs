using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId}/test-cases")]
public class TestCasesController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TestCasesController> _logger;

    public TestCasesController(TestifyDbContext db, IAuditLogService auditLogService, ILogger<TestCasesController> logger)
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
    public async Task<IActionResult> GetTestCases(Guid projectId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var testCases = await _db.TestCases
            .Where(tc => tc.ProjectId == projectId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return Ok(testCases);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestCase(Guid projectId, Guid id)
    {
        var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == id && tc.ProjectId == projectId);
        if (testCase == null)
            return NotFound();

        return Ok(testCase);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTestCase(Guid projectId, [FromBody] CreateTestCaseRequest req)
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

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = new { message = "Title is required", code = "INVALID_TITLE" }, status = 400 });

        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = req.Title,
            Preconditions = req.Preconditions,
            Steps = req.Steps,
            ExpectedResult = req.ExpectedResult,
            Priority = req.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TestCases.Add(testCase);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "create_test_case",
            "test_case",
            testCase.Id,
            projectId,
            new Dictionary<string, object> { { "title", testCase.Title } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetTestCase), new { projectId, id = testCase.Id }, testCase);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTestCase(Guid projectId, Guid id, [FromBody] UpdateTestCaseRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == id && tc.ProjectId == projectId);
        if (testCase == null)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = new { message = "Title is required", code = "INVALID_TITLE" }, status = 400 });

        testCase.Title = req.Title;
        testCase.Preconditions = req.Preconditions;
        testCase.Steps = req.Steps;
        testCase.ExpectedResult = req.ExpectedResult;
        testCase.Priority = req.Priority;
        testCase.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "update_test_case",
            "test_case",
            testCase.Id,
            projectId,
            new Dictionary<string, object> { { "title", testCase.Title } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(testCase);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTestCase(Guid projectId, Guid id)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == id && tc.ProjectId == projectId);
        if (testCase == null)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null || member.Role == "viewer")
            return Forbid();

        _db.TestCases.Remove(testCase);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "delete_test_case",
            "test_case",
            testCase.Id,
            projectId,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }
}

public class CreateTestCaseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Preconditions { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}

public class UpdateTestCaseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Preconditions { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}