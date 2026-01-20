namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// Filter fuer das Alter von Immobilien-Eintraegen
/// </summary>
public enum AgeFilter
{
    /// <summary>
    /// Kein Filter - alle Eintraege anzeigen
    /// </summary>
    Alle,

    /// <summary>
    /// Eintraege der letzten 24 Stunden
    /// </summary>
    EinTag,

    /// <summary>
    /// Eintraege der letzten 7 Tage
    /// </summary>
    EineWoche,

    /// <summary>
    /// Eintraege der letzten 30 Tage
    /// </summary>
    EinMonat,

    /// <summary>
    /// Eintraege des letzten Jahres
    /// </summary>
    EinJahr
}
