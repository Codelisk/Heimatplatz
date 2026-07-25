using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Misst, wie lange es vom Antippen einer Karte bis zur fertigen Detailseite dauert,
/// und protokolliert die Zwischenschritte. Ohne diese Marken laesst sich nicht sagen,
/// ob Netzwerk, Seitenaufbau oder Bild der Engpass ist - jede Optimierung waere Raten.
///
/// Ausgabe (ein Log-Eintrag je Schritt, Ebene Information wie die uebrigen
/// Navigations-Logs - auf Debug waeren die Marken im Standard-Log unsichtbar):
/// <c>[DetailNav] Seite sichtbar nach 132 ms</c>
/// </summary>
[Singleton]
public sealed class DetailNavigationTrace(ILogger<DetailNavigationTrace> logger)
{
    private readonly Stopwatch _stopwatch = new();
    private Guid _propertyId;

    /// <summary>Tap auf die Karte - startet die Messung.</summary>
    public void Start(Guid propertyId)
    {
        _propertyId = propertyId;
        _stopwatch.Restart();
        logger.LogInformation("[DetailNav] Tap auf {PropertyId}", propertyId);
    }

    /// <summary>Ein Zwischenschritt der aktuell gemessenen Navigation.</summary>
    public void Mark(Guid propertyId, string step)
    {
        if (!_stopwatch.IsRunning || propertyId != _propertyId)
            return;

        logger.LogInformation(
            "[DetailNav] {Step} nach {Elapsed} ms",
            step,
            _stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Letzter Schritt - beendet die Messung.</summary>
    public void Complete(Guid propertyId, string step)
    {
        if (!_stopwatch.IsRunning || propertyId != _propertyId)
            return;

        _stopwatch.Stop();
        logger.LogInformation(
            "[DetailNav] {Step} nach {Elapsed} ms (fertig)",
            step,
            _stopwatch.ElapsedMilliseconds);
    }
}
