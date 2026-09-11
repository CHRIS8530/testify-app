using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId}/members")]
public class ProjectMembersController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProjectMembersController> _logger;

    public ProjectMembersController(
        TestifyDbContext db,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<ProjectMembersController> logger)
    {
        _db = db;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember(Guid projectId, [FromBody] InviteMemberRequest req)
    {
        var userId = GetUserIdFromToken();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        // Check if project exists and user is owner
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

        if (project.OwnerId != userId)
            return Forbid();

        // Validate email
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = new { message = "Email is required", code = "INVALID_EMAIL" }, status = 400 });

        // Check if email is registered
        var invitedUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (invitedUser == null)
            return BadRequest(new { error = new { message = "Email not registered", code = "USER_NOT_FOUND" }, status = 400 });

        // Check if user is already a member
        if (await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == invitedUser.Id))
            return BadRequest(new { error = new { message = "User already a member of this project", code = "ALREADY_MEMBER" }, status = 400 });

        // Validate role
        if (string.IsNullOrWhiteSpace(req.Role) || !new[] { "editor", "viewer" }.Contains(req.Role))
            return BadRequest(new { error = new { message = "Invalid role. Must be 'editor' or 'viewer'", code = "INVALID_ROLE" }, status = 400 });

        // Add member
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = invitedUser.Id,
            Role = req.Role,
            AddedAt = DateTime.UtcNow
        };

        _db.ProjectMembers.Add(member);

        // Generate and store invite token
        var inviteToken = Guid.NewGuid().ToString();

        await _db.SaveChangesAsync();

        // Send invitation email
        await _emailService.SendInvitationEmailAsync(invitedUser.Email, project.Name, req.Role, inviteToken);

        await _auditLogService.LogActionAsync(
            userId,
            "invite_member",
            "project_member",
            member.Id,
            projectId,
            new Dictionary<string, object> { { "invited_user_email", invitedUser.Email }, { "role", req.Role } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(InviteMember), new { projectId, memberId = member.Id }, new
        {
            id = member.Id,
            userId = invitedUser.Id,
            email = invitedUser.Email,
            role = member.Role,
            addedAt = member.AddedAt
        });
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId)
    {
        var requestingUserId = GetUserIdFromToken();
        if (requestingUserId == Guid.Empty)
            return Unauthorized(new { error = new { message = "Unauthorized", code = "UNAUTHORIZED" }, status = 401 });

        // Check if project exists and user is owner
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

        if (project.OwnerId != requestingUserId)
            return Forbid();

        // Check if member exists
        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member == null)
            return NotFound(new { error = new { message = "Member not found", code = "NOT_FOUND" }, status = 404 });

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            requestingUserId,
            "remove_member",
            "project_member",
            member.Id,
            projectId,
            new Dictionary<string, object> { { "removed_user_id", userId } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}