using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace Heimatplatz.Api.Core.Startup;

/// <summary>
/// Zentrales Fehler-Logging fuer TickerQ-Hintergrund-Jobs. Wird bei JEDEM fehlgeschlagenen
/// Versuch aufgerufen (auch vor Retries) - die fachliche Fehlerbehandlung (z.B. Status
/// "Failed" am Inserat-Entwurf) machen die Jobs selbst beim letzten Versuch.
/// </summary>
public class TickerQJobExceptionHandler(ILogger<TickerQJobExceptionHandler> logger) : ITickerExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        logger.LogError(exception, "[TickerQ] Job {TickerId} ({TickerType}) fehlgeschlagen", tickerId, tickerType);
        return Task.CompletedTask;
    }

    public Task HandleCanceledExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        logger.LogWarning("[TickerQ] Job {TickerId} ({TickerType}) abgebrochen", tickerId, tickerType);
        return Task.CompletedTask;
    }
}
