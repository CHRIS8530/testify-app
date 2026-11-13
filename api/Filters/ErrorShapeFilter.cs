using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Testify.Api.Filters;

// Errors the framework makes itself (malformed JSON, an id that is not a Guid, a bare NotFound()) come out as
// ProblemDetails, which has a different shape from the errors the controllers write. This filter rewrites them
// into the one standard shape: { "error": { "message", "code" }, "status": 4xx }.
// It implements IAlwaysRunResultFilter because the automatic 400 for a bad request is produced by an action filter
// that short-circuits, and an ordinary result filter would not see it.
public class ErrorShapeFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        int? status = context.Result switch
        {
            ObjectResult { Value: ProblemDetails problem } objectResult => problem.Status ?? objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };

        if (status is null or < 400)
            return;

        var (message, code) = Describe(status.Value);
        context.Result = new ObjectResult(new { error = new { message, code }, status = status.Value })
        {
            StatusCode = status.Value
        };
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static (string Message, string Code) Describe(int status) => status switch
    {
        400 => ("The request is not valid", "INVALID_REQUEST"),
        401 => ("Unauthorized", "UNAUTHORIZED"),
        403 => ("You do not have access to this resource", "FORBIDDEN"),
        404 => ("Not found", "NOT_FOUND"),
        405 => ("Method not allowed", "METHOD_NOT_ALLOWED"),
        409 => ("Conflict", "CONFLICT"),
        415 => ("Unsupported media type", "UNSUPPORTED_MEDIA_TYPE"),
        429 => ("Too many requests", "TOO_MANY_REQUESTS"),
        >= 500 => ("Something went wrong", "SERVER_ERROR"),
        _ => ("The request could not be completed", "REQUEST_FAILED")
    };
}