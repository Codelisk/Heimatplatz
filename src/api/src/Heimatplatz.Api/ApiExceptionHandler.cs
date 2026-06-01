using Heimatplatz.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Heimatplatz.Api;

/// <summary>
/// Behandelt fachliche <see cref="ApiException"/>s (z. B. Konflikt/Validierung) und gibt
/// einen definierten HTTP-Statuscode samt ProblemDetails zurueck, statt eines generischen 500.
/// </summary>
public class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger
) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            return false;
        }

        logger.LogWarning(apiException, "{Title} ({StatusCode}): {Message}",
            apiException.Title, apiException.StatusCode, apiException.Message);

        httpContext.Response.StatusCode = apiException.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = apiException.StatusCode,
            Title = apiException.Title,
            Detail = apiException.Message
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
