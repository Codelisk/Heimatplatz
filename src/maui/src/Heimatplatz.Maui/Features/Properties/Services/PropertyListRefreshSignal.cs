namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Einmal-Signal "eigene Inserate haben sich geaendert": Nach Veroeffentlichen/
/// Bearbeiten im Wizard wuerde "Meine Immobilien" sonst den bis zu 15 Minuten
/// alten LocalFirst-Cache anzeigen und das neue Inserat fehlt. Der naechste
/// Listen-Load konsumiert das Signal und laedt einmalig am Cache vorbei.
/// </summary>
public static class PropertyListRefreshSignal
{
    private static int _pending;

    /// <summary>Meldet eine Aenderung an den eigenen Inseraten an.</summary>
    public static void Request() => Interlocked.Exchange(ref _pending, 1);

    /// <summary>Liefert true genau einmal pro angemeldeter Aenderung.</summary>
    public static bool Consume() => Interlocked.Exchange(ref _pending, 0) == 1;
}
