using Microsoft.AspNetCore.Mvc;

namespace Heimatplatz.Api;

/// <summary>
/// Sicherheitsnetz fuer Handler, die noch BCL-Exceptions statt der fachlichen
/// <see cref="Heimatplatz.Api.Exceptions.ApiException"/>-Typen werfen:
/// <see cref="ArgumentException"/> (Validierung) wird zu HTTP 400,
/// <see cref="KeyNotFoundException"/> (Ressource fehlt) zu HTTP 404 -
/// statt beides als generisches 500 an den Client zu melden.
/// Neue Handler sollen ValidationException/NotFoundException verwenden.
/// </summary>
public class LegacyExceptionHandler(
    ILogger<LegacyExceptionHandler> logger
) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Validation Failed"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "{Title} ({StatusCode}): {Message}",
            title, statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
