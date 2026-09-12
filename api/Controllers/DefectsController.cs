using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId}/defects")]
public class DefectsController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DefectsController> _logger;

    public DefectsController(TestifyDbContext db, IAuditLogService auditLogService, ILogger<DefectsController> logger)
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
    public async Task<IActionResult> GetDefects(Guid projectId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var testCaseIds = await _db.TestCases
            .Where(tc => tc.ProjectId == projectId)
            .Select(tc => tc.Id)
            .ToListAsync();

        var defects = await _db.Defects
            .Where(d => testCaseIds.Contains(d.TestCaseId))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return Ok(defects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDefect(Guid projectId, Guid id)
    {
        var testCaseIds = await _db.TestCases
            .Where(tc => tc.ProjectId == projectId)
            .Select(tc => tc.Id)
            .ToListAsync();

        var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == id && testCaseIds.Contains(d.TestCaseId));
        if (defect == null)
            return NotFound();

        return Ok(defect);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDefect(Guid projectId, [FromBody] CreateDefectRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = new { message = "Title is required", code = "INVALID_TITLE" }, status = 400 });

        var defect = new Defect
        {
            Id = Guid.NewGuid(),
            TestCaseId = req.TestCaseId,
            TestRunResultId = req.TestRunResultId,
            Title = req.Title,
            Description = req.Description ?? string.Empty,
            Severity = req.Severity ?? "Medium",
            Status = "open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Defects.Add(defect);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "create_defect",
            "defect",
            defect.Id,
            projectId,
            new Dictionary<string, object> { { "title", defect.Title }, { "severity", defect.Severity } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetDefect), new { projectId, id = defect.Id }, defect);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDefect(Guid projectId, Guid id, [FromBody] UpdateDefectRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == id);
        if (defect == null)
            return NotFound();

        var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == defect.TestCaseId);
        if (testCase?.ProjectId != projectId)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null)
            return Forbid();

        defect.Status = req.Status;
        defect.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "update_defect",
            "defect",
            defect.Id,
            projectId,
            new Dictionary<string, object> { { "status", defect.Status } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(defect);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDefect(Guid projectId, Guid id)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == id);
        if (defect == null)
            return NotFound();

        var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == defect.TestCaseId);
        if (testCase?.ProjectId != projectId)
            return NotFound();

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null)
            return Forbid();

        _db.Defects.Remove(defect);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "delete_defect",
            "defect",
            defect.Id,
            projectId,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }
}

public class CreateDefectRequest
{
    public Guid TestCaseId { get; set; }
    public Guid TestRunResultId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Severity { get; set; }
}

public class UpdateDefectRequest
{
    public string Status { get; set; } = string.Empty;
}