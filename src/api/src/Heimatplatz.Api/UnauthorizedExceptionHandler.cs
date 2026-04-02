using Microsoft.AspNetCore.Mvc;

namespace Heimatplatz.Api;

/// <summary>
/// Handles <see cref="UnauthorizedAccessException"/> by returning a 401 ProblemDetails response.
/// </summary>
public class UnauthorizedExceptionHandler(
    ILogger<UnauthorizedExceptionHandler> logger
) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedAccessException unauthorizedException)
        {
            return false;
        }

        logger.LogWarning(unauthorizedException, "Unauthorized access: {Message}", unauthorizedException.Message);

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = unauthorizedException.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
