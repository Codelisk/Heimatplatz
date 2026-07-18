namespace Heimatplatz.Api.Features.PropertyDrafts.Infrastructure;

/// <summary>
/// Platzhalter-Beschreibung fuer Inserate, die veroeffentlicht werden, waehrend der
/// Beschreibungs-Job noch laeuft. Muss die CreateProperty-Mindestlaenge (50 Zeichen)
/// erfuellen; der Job ersetzt den Text nach Abschluss (siehe DraftDescriptionProcessor).
/// </summary>
public static class DraftDescriptionPlaceholder
{
    public const string Text =
        "Die ausführliche Beschreibung wird gerade erstellt und erscheint hier in Kürze.";
}
