using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(TestifyDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        var requestingUserId = GetUserIdFromToken();
        if (requestingUserId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        // A user can read audit entries for their own actions and for any project they own.
        // There is no admin role, so nobody can read the activity of unrelated users.
        var ownedProjectIds = _db.Projects.Where(p => p.OwnerId == requestingUserId).Select(p => (Guid?)p.Id);
        var query = _db.AuditLogs.Where(al => al.UserId == requestingUserId || ownedProjectIds.Contains(al.ProjectId));

        if (projectId.HasValue)
            query = query.Where(al => al.ProjectId == projectId);

        if (userId.HasValue)
            query = query.Where(al => al.UserId == userId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(al => al.Action == action);

        if (startDate.HasValue)
            query = query.Where(al => al.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(al => al.Timestamp <= endDate.Value);

        // Get total count
        var total = await query.CountAsync();

        // Apply pagination
        var logs = await query
            .OrderByDescending(al => al.Timestamp)
            .Skip(offset)
            .Take(limit)
            .Include(al => al.User)
            .Include(al => al.Project)
            .Select(al => new
            {
                al.Id,
                al.UserId,
                User = al.User != null ? new { al.User.Id, al.User.Email, al.User.Username } : null,
                al.Action,
                al.ResourceType,
                al.ResourceId,
                al.ProjectId,
                Project = al.Project != null ? new { al.Project.Id, al.Project.Name } : null,
                al.Details,
                al.IpAddress,
                al.Timestamp
            })
            .ToListAsync();

        return Ok(new
        {
            logs,
            total,
            limit,
            offset
        });
    }
}