using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Testify.Api.Data;
using Testify.Api.Services;

namespace Testify.Api.Controllers;

public static class ControllerAccessExtensions
{
    private static IActionResult Error(ControllerBase controller, int status, string message, string code)
    {
        return controller.StatusCode(status, new { error = new { message, code }, status });
    }

    // A 403 in the same error shape as every other endpoint. ControllerBase.Forbid cannot be used,
    // because no authentication scheme is registered, which turned every "not allowed" into a 500.
    public static IActionResult ForbiddenResponse(this ControllerBase controller)
    {
        return Error(controller, StatusCodes.Status403Forbidden, "You do not have access to this resource", "FORBIDDEN");
    }

    // Returns no error when the signed in user has at least the minimum role on the project.
    // Otherwise it returns the 401, 404 or 403 to send back.
    public static async Task<(IActionResult? Denied, Guid UserId)> RequireProjectAccessAsync(this ControllerBase controller, TestifyDbContext db, Guid projectId, ProjectRole minimum)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            return (Error(controller, StatusCodes.Status401Unauthorized, "Unauthorized", "UNAUTHORIZED"), Guid.Empty);

        var role = await ProjectAccess.GetRoleAsync(db, projectId, userId);

        if (role == ProjectRole.NotFound)
            return (Error(controller, StatusCodes.Status404NotFound, "Project not found", "NOT_FOUND"), userId);

        if (role < minimum)
            return (controller.ForbiddenResponse(), userId);

        return (null, userId);
    }
}
