using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;

namespace Testify.Api.Services;

public enum ProjectRole
{
    NotFound = 0,
    None = 1,
    Viewer = 2,
    Editor = 3,
    Owner = 4
}

public static class ProjectAccess
{
    // The owner is whoever Project.OwnerId says. Everyone else is an invited member (editor or viewer) or has no access.
    public static async Task<ProjectRole> GetRoleAsync(TestifyDbContext db, Guid projectId, Guid userId)
    {
        var ownerId = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => (Guid?)p.OwnerId)
            .FirstOrDefaultAsync();

        if (ownerId is null)
            return ProjectRole.NotFound;

        if (ownerId == userId)
            return ProjectRole.Owner;

        var membership = await db.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
            .Select(pm => new { pm.Role })
            .FirstOrDefaultAsync();

        if (membership is null)
            return ProjectRole.None;

        return string.Equals(membership.Role, "viewer", StringComparison.OrdinalIgnoreCase) ? ProjectRole.Viewer : ProjectRole.Editor;
    }
}
