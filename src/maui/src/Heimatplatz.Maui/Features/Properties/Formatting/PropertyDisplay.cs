using System.Globalization;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.DependencyInjection;

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

    // Localized Status-Texte per Service-Locator - PropertyDisplay ist statisch
    // (Aufrufer erwarten die statische API), lazy aufgeloest und gecacht wie in
    // PropertyCard.xaml.cs. GetService statt GetRequiredService: solange die
    // Services noch nicht bereitstehen, faellt die Anzeige auf den Rohwert zurueck.
    private static PropertyDisplayStringsLocalized? _locCache;
    private static PropertyDisplayStringsLocalized? Loc =>
        _locCache ??= IPlatformApplication.Current?.Services.GetService<PropertyDisplayStringsLocalized>();

    /// <summary>
    /// Lokalisierte Bezeichnung des Verfahrens-Status - zentral, damit
    /// PropertyDetail- und ForeclosureDetail-Seite identisch formulieren.
    /// </summary>
    public static string LegalStatusText(LegalStatus status) => LegalStatusText(status.ToString());

    /// <summary>
    /// String-Variante fuer Status-Werte direkt aus TypeSpecificData-JSON
    /// (kann auch serverseitige Werte ausserhalb des lokalen Enums enthalten).
    /// </summary>
    public static string LegalStatusText(string status) => Loc is not { } loc ? status : status switch
    {
        "Pending" => loc.LegalStatusPending,
        "Scheduled" => loc.LegalStatusScheduled,
        "InProgress" => loc.LegalStatusInProgress,
        "Completed" => loc.LegalStatusCompleted,
        "Cancelled" => loc.LegalStatusCancelled,
        "Suspended" => loc.LegalStatusSuspended,
        _ => status
    };
}
