using System.Globalization;
using System.Text.RegularExpressions;
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
    public static readonly CultureInfo Culture = CreateAustrianCulture();

    /// <summary>
    /// ICU/CLDR gruppiert de-AT-Zahlen mit geschuetztem Leerzeichen ("520 000"),
    /// Windows-NLS mit Punkt ("520.000") - fuer eine plattformuebergreifend
    /// identische Anzeige werden die Trennzeichen fest verdrahtet.
    /// </summary>
    private static CultureInfo CreateAustrianCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("de-AT").Clone();
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        return culture;
    }

    /// <summary>"€ 3.590.000" - Symbol vorn wie am Web (formatApiPrice)</summary>
    public static string Price(decimal value) => string.Format(Culture, "€ {0:N0}", value);

    /// <summary>"1.234 m²"</summary>
    public static string Area(decimal value) => string.Format(Culture, "{0:N0} m²", value);

    /// <summary>"1.234"</summary>
    public static string Number(decimal value) => string.Format(Culture, "{0:N0}", value);

    /// <summary>
    /// Sichtbarer Anzeigetitel: Aus Zwangsversteigerungen synchronisierte Inserate
    /// tragen den Titel "Zwangsversteigerung: Mehrfamilienhaus ..." - das Praefix
    /// ist neben dem ZV-Badge redundant (Web: getDisplayTitle in format.ts).
    /// </summary>
    public static string DisplayTitle(string? title)
    {
        var trimmed = (title ?? "").Trim();
        var stripped = Regex.Replace(trimmed, @"^\s*Zwangsversteigerung\s*[:\-–—]\s*", "", RegexOptions.IgnoreCase).Trim();
        return stripped.Length > 0 ? stripped : trimmed;
    }

    /// <summary>"4742 Pram" - die PLZ gehoert vor den Ort, nie vor die Strasse.</summary>
    public static string LocationLine(string? postalCode, string? city) =>
        string.Join(" ", new[] { postalCode?.Trim(), city?.Trim() }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>
    /// Strassenzeile ohne die Ortsteile, die Importe oft mitliefern ("4742 Pram",
    /// "Pram Bahnhofstrasse 3", "Bahnhofstrasse 3, 4742 Pram"). Liefert "" wenn
    /// die Adresse nichts ueber PLZ+Ort hinaus enthaelt (Web: getApiStreetLine).
    /// </summary>
    public static string StreetLine(string? address, string? postalCode, string? city)
    {
        var street = (address ?? "").Trim();
        if (street.Length == 0)
            return "";

        var pc = Regex.Escape((postalCode ?? "").Trim());
        var ct = Regex.Escape((city ?? "").Trim());

        if (pc.Length > 0)
            street = Regex.Replace(street, $@"^{pc}(?:[\s,]+|$)", "", RegexOptions.IgnoreCase).Trim();

        if (ct.Length > 0)
        {
            // Nachgestelltes ", (PLZ) Ort" abschneiden
            var trailing = pc.Length > 0 ? $@"[\s,]+(?:{pc}\s+)?{ct}$" : $@"[\s,]+{ct}$";
            street = Regex.Replace(street, trailing, "", RegexOptions.IgnoreCase).Trim();

            // Fuehrenden Ortsnamen nur strippen, wenn danach keine blosse Hausnummer
            // folgt - "Pram 44" IST die Strassenangabe in Doerfern ohne Strassennamen
            var rest = Regex.Replace(street, $@"^{ct}(?:[\s,]+|$)", "", RegexOptions.IgnoreCase).Trim();
            if (rest != street && (rest.Length == 0 || !char.IsDigit(rest[0])))
                street = rest;
        }

        return street;
    }

    // Kalendertage werden in oesterreichischer Ortszeit verglichen (wie am Web-Zettel,
    // viennaDayValue in format.ts) - eine reine Differenz der Zeitstempel zaehlt je
    // nach Uhrzeit einen Tag zu viel oder zu wenig und ist nicht DST-fest.
    // Fallback auf die Geraete-Zeitzone, falls die IANA-Id nicht aufloesbar ist.
    private static readonly TimeZoneInfo AustrianTimeZone = ResolveAustrianTimeZone();

    private static TimeZoneInfo ResolveAustrianTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static DateTime AustrianDay(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, AustrianTimeZone).Date;

    /// <summary>
    /// Volle Kalendertage bis zum Versteigerungstermin (0 = heute, 1 = morgen).
    /// Liefert null fuer fehlende, unplausible (Jahr &lt;= 1900, wie die API-seitige
    /// ResolveAuctionDate) oder vergangene Termine - die Karte zeigt dann keinen
    /// Countdown (Web: getAuctionCountdownLabel in format.ts).
    /// </summary>
    public static int? AuctionCountdownDays(DateTimeOffset? auctionDate)
    {
        if (auctionDate is not { } value || value.Year <= 1900)
            return null;

        var days = (AustrianDay(value) - AustrianDay(DateTimeOffset.UtcNow)).Days;
        return days < 0 ? null : days;
    }

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
