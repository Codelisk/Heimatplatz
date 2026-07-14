using System.Globalization;
using Heimatplatz.Maui.Features.Properties.Models;

namespace Heimatplatz.Maui.Features.Properties;

/// <summary>
/// Einheitliche oesterreichische Anzeige-Formatierung fuer Immobilien-Daten
/// (Karten + Detailseiten), unabhaengig von der Geraete-Culture.
/// Ersetzt den frueheren ".Replace(",", ".")"-Hack, der Dezimalwerte (N2) zerstoert hat.
/// </summary>
public static class PropertyDisplay
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-AT");

    /// <summary>"3.590.000 €"</summary>
    public static string Price(decimal value) => string.Format(Culture, "{0:N0} €", value);

    /// <summary>"2.500,00 €" (z.B. Preis pro m²)</summary>
    public static string PriceExact(decimal value) => string.Format(Culture, "{0:N2} €", value);

    /// <summary>"1.234 m²"</summary>
    public static string Area(decimal value) => string.Format(Culture, "{0:N0} m²", value);

    /// <summary>"1.234"</summary>
    public static string Number(decimal value) => string.Format(Culture, "{0:N0}", value);

    /// <summary>
    /// Deutsche Bezeichnung des Verfahrens-Status - zentral, damit
    /// PropertyDetail- und ForeclosureDetail-Seite identisch formulieren.
    /// </summary>
    public static string LegalStatusText(LegalStatus status) => LegalStatusText(status.ToString());

    /// <summary>
    /// String-Variante fuer Status-Werte direkt aus TypeSpecificData-JSON
    /// (kann auch serverseitige Werte ausserhalb des lokalen Enums enthalten).
    /// </summary>
    public static string LegalStatusText(string status) => status switch
    {
        "Pending" => "Anhängig",
        "Scheduled" => "Terminiert",
        "InProgress" => "Laufend",
        "Completed" => "Abgeschlossen",
        "Cancelled" => "Aufgehoben",
        "Suspended" => "Ausgesetzt",
        _ => status
    };
}
