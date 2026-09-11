using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
public class ProjectsController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(TestifyDbContext db, IAuditLogService auditLogService, ILogger<ProjectsController> logger)
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
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _db.Projects.ToListAsync();
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return NotFound();

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = new { message = "Name is required", code = "INVALID_NAME" }, status = 400 });

        if (req.Name.Length > 255)
            return BadRequest(new { error = new { message = "Name must be 255 characters or less", code = "INVALID_NAME" }, status = 400 });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description ?? string.Empty,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "create_project",
            "project",
            project.Id,
            project.Id,
            new Dictionary<string, object> { { "name", project.Name } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return NotFound();

        if (project.OwnerId != userId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = new { message = "Name is required", code = "INVALID_NAME" }, status = 400 });

        if (req.Name.Length > 255)
            return BadRequest(new { error = new { message = "Name must be 255 characters or less", code = "INVALID_NAME" }, status = 400 });

        project.Name = req.Name;
        project.Description = req.Description ?? string.Empty;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "update_project",
            "project",
            project.Id,
            project.Id,
            new Dictionary<string, object> { { "name", project.Name } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return NotFound();

        if (project.OwnerId != userId)
            return Forbid();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            userId,
            "delete_project",
            "project",
            project.Id,
            project.Id,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}