using System.Diagnostics;

namespace Heimatplatz.Api;

/// <summary>
/// Letzter Handler in der Kette: loggt alle Exceptions, die keiner der fachlichen
/// Handler behandelt hat (= unbehandelte 500er), mit Pfad und TraceId als LogError -
/// damit landen sie inkl. Fehlergruppe in der Telemetrie. Gibt bewusst false zurueck,
/// die ProblemDetails-500-Antwort erzeugt weiterhin die Standard-Middleware.
/// </summary>
public class FallbackExceptionHandler(
    ILogger<FallbackExceptionHandler> logger
) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception,
            "Unbehandelte Exception bei {Method} {Path} (TraceId {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);

        return ValueTask.FromResult(false);
    }
}
