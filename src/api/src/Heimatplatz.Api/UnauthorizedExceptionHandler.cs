using Microsoft.AspNetCore.Diagnostics;

namespace Heimatplatz.Api;

public class UnauthorizedExceptionHandler : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = exception.Message },
                cancellationToken);
            return true;
        }

        return false;
    }
}
