using System.Globalization;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Heimatplatz.Api.Features.Firmenbuch.Configuration;
using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.Firmenbuch.Services;

/// <summary>
/// Client fuer die amtliche "FBW-WebServices (HVD)"-Schnittstelle des Bundesministeriums fuer
/// Justiz (SOAP 1.2, X-API-KEY-Header = JustizOnline-IWG-Zugriffstoken). Liefert zu einer
/// Firmenbuchnummer (FNR) amtliche Daten: praezises Gruendungsdatum (DATERST, Fallback
/// fruehestes Vollzugsdatum), EUID und Geschaeftsfuehrung samt Geburtsdatum. Kein WSDL/SOAP-Toolchain
/// im Einsatz - Request/Response werden direkt per XDocument gebaut/geparst (Antwort-Elemente
/// werden ueber LocalName statt exakter Namespace-Praefixe gesucht, da die Schnittstelle in
/// jeder Antwort alle bekannten Namespaces deklariert, unabhaengig von der aufgerufenen
/// Operation).
/// </summary>
public class FirmenbuchHvdClient(
    HttpClient httpClient,
    IOptions<FirmenbuchHvdOptions> options,
    ILogger<FirmenbuchHvdClient> logger
) : IFirmenbuchHvdClient
{
    private const string AuszugRequestNs = "ns://firmenbuch.justiz.gv.at/Abfrage/v2/AuszugRequest";
    private const string SucheFirmaRequestNs = "ns://firmenbuch.justiz.gv.at/Abfrage/SucheFirmaRequest";
    private const string SoapNs = "http://www.w3.org/2003/05/soap-envelope";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<FirmenbuchAuszug?> GetAuszugAsync(string fnr, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return null;

        XNamespace soap = SoapNs;
        XNamespace aus = AuszugRequestNs;

        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", SoapNs),
            new XAttribute(XNamespace.Xmlns + "aus", AuszugRequestNs),
            new XElement(soap + "Header"),
            new XElement(soap + "Body",
                new XElement(aus + "AUSZUG_V2_REQUEST",
                    new XElement(aus + "FNR", NormalizeFnr(fnr)),
                    new XElement(aus + "STICHTAG", DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement(aus + "UMFANG", "Kurzinformation"))));

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.BaseUrl)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml") { CharSet = "UTF-8" };
        request.Headers.Add("X-API-KEY", options.Value.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Firmenbuch-HVD-Anfrage fuer FNR {Fnr} lieferte HTTP {StatusCode}", fnr, (int)response.StatusCode);
                return null;
            }

            var document = XDocument.Parse(body);

            var fault = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault != null)
            {
                var reason = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "Text")?.Value;
                logger.LogWarning("Firmenbuch-HVD SOAP-Fault fuer FNR {Fnr}: {Reason}", fnr, reason ?? "unbekannt");
                return null;
            }

            var auszug = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "AUSZUG_V2_RESPONSE");
            if (auszug == null)
            {
                logger.LogWarning("Firmenbuch-HVD-Antwort fuer FNR {Fnr} enthielt kein AUSZUG_V2_RESPONSE", fnr);
                return null;
            }

            return ParseAuszug(auszug);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Fehler bei Firmenbuch-HVD-Anfrage fuer FNR {Fnr}", fnr);
            return null;
        }
    }

    public async Task<List<FirmenbuchSearchResult>?> SearchAsync(string wortlaut, string ortNr = "", CancellationToken ct = default)
    {
        if (!IsConfigured)
            return null;

        XNamespace soap = SoapNs;
        XNamespace su = SucheFirmaRequestNs;

        // SUCHBEREICH 1 = eingetragene UND geloeschte Firmen (keine Zweigniederlassungen) -
        // bewusst inklusive geloeschter, der Status steht am Treffer und wird mitgespeichert.
        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", SoapNs),
            new XAttribute(XNamespace.Xmlns + "su", SucheFirmaRequestNs),
            new XElement(soap + "Header"),
            new XElement(soap + "Body",
                new XElement(su + "SUCHEFIRMAREQUEST",
                    new XElement(su + "FIRMENWORTLAUT", wortlaut),
                    new XElement(su + "EXAKTESUCHE", "true"),
                    new XElement(su + "SUCHBEREICH", "1"),
                    new XElement(su + "GERICHT", string.Empty),
                    new XElement(su + "RECHTSFORM", string.Empty),
                    new XElement(su + "RECHTSEIGENSCHAFT", string.Empty),
                    new XElement(su + "ORTNR", ortNr))));

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.BaseUrl)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml") { CharSet = "UTF-8" };
        request.Headers.Add("X-API-KEY", options.Value.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Firmenbuch-Suche '{Wortlaut}' (OrtNr={OrtNr}) lieferte HTTP {StatusCode}", wortlaut, ortNr, (int)response.StatusCode);
                return null;
            }

            var document = XDocument.Parse(body);

            var fault = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault != null)
            {
                var reason = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "Text")?.Value;
                logger.LogWarning("Firmenbuch-Suche '{Wortlaut}' (OrtNr={OrtNr}) SOAP-Fault: {Reason}", wortlaut, ortNr, reason ?? "unbekannt");
                return null;
            }

            return ParseSearchResults(document);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Fehler bei Firmenbuch-Suche '{Wortlaut}' (OrtNr={OrtNr})", wortlaut, ortNr);
            return null;
        }
    }

    internal static List<FirmenbuchSearchResult> ParseSearchResults(XDocument document)
    {
        var results = new List<FirmenbuchSearchResult>();
        foreach (var ergebnis in document.Descendants().Where(e => e.Name.LocalName == "ERGEBNIS"))
        {
            var fnr = ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "FNR")?.Value;
            if (string.IsNullOrWhiteSpace(fnr))
                continue;

            // NAME kann mehrzeilig sein (mehrere NAME-Elemente pro Treffer) - zusammenfuegen
            var name = string.Join(" ", ergebnis.Elements()
                .Where(e => e.Name.LocalName == "NAME")
                .Select(e => e.Value.Trim())
                .Where(v => v.Length > 0));
            if (name.Length == 0)
                continue;

            var rechtsform = ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "RECHTSFORM");
            var gericht = ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "GERICHT");

            results.Add(new FirmenbuchSearchResult
            {
                Fnr = NormalizeFnr(fnr),
                Name = name,
                Status = NullIfEmpty(ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "STATUS")?.Value),
                Sitz = NullIfEmpty(ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "SITZ")?.Value),
                RechtsformCode = NullIfEmpty(rechtsform?.Elements().FirstOrDefault(e => e.Name.LocalName == "CODE")?.Value),
                RechtsformText = NullIfEmpty(rechtsform?.Elements().FirstOrDefault(e => e.Name.LocalName == "TEXT")?.Value),
                Rechtseigenschaft = NullIfEmpty(ergebnis.Elements().FirstOrDefault(e => e.Name.LocalName == "RECHTSEIGENSCHAFT")?.Value),
                GerichtCode = NullIfEmpty(gericht?.Elements().FirstOrDefault(e => e.Name.LocalName == "CODE")?.Value),
                GerichtText = NullIfEmpty(gericht?.Elements().FirstOrDefault(e => e.Name.LocalName == "TEXT")?.Value)
            });
        }

        return results;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static FirmenbuchAuszug ParseAuszug(XElement auszug)
    {
        var euidContainer = auszug.Elements().FirstOrDefault(e => e.Name.LocalName == "EUID");
        var euid = euidContainer?.Elements().FirstOrDefault(e => e.Name.LocalName == "EUID")?.Value;

        // Gruendungsdatum: bevorzugt das amtliche Ersteintragungsdatum (DATERST in FI_DKZ09,
        // yyyyMMdd) - es reicht in die Handelsregister-Aera vor 1991 zurueck. Das aelteste
        // Vollzugsdatum ist nur Fallback: bei Altfirmen ist das lediglich die FBG-Ersterfassung
        // (1994+), nicht die Gruendung.
        var erstDates = auszug.Descendants()
            .Where(e => e.Name.LocalName == "DATERST")
            .Select(e => ParseCompactDate(e.Value))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var vollzugsDates = auszug.Elements()
            .Where(e => e.Name.LocalName == "VOLLZ")
            .Select(e => e.Elements().FirstOrDefault(c => c.Name.LocalName == "VOLLZUGSDATUM")?.Value)
            .Select(ParseDate)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        DateOnly? foundedDate = erstDates.Count > 0 ? erstDates.Min()
            : vollzugsDates.Count > 0 ? vollzugsDates.Min()
            : null;

        // Rollentext je Personennummer (PNR, z.B. "  A") aus den FUN-Eintraegen sammeln,
        // um ihn anschliessend den PER-Eintraegen (selbe PNR) zuzuordnen. Die echte
        // Schnittstelle liefert PNR/FKENTEXT als ATTRIBUTE am FUN/PER-Element (live am
        // 22.7.2026 verifiziert), das XSD legt Kind-Elemente nahe - beide Formen matchen.
        var roleByPnr = new Dictionary<string, string>();
        foreach (var fun in auszug.Elements().Where(e => e.Name.LocalName == "FUN"))
        {
            var pnr = NormalizePnr(AttributeOrElementValue(fun, "PNR"));
            var roleText = AttributeOrElementValue(fun, "FKENTEXT");
            if (pnr != null && !string.IsNullOrWhiteSpace(roleText))
                roleByPnr[pnr] = roleText;
        }

        var people = new List<FirmenbuchPerson>();
        foreach (var per in auszug.Elements().Where(e => e.Name.LocalName == "PER"))
        {
            var peDkz02 = per.Elements().FirstOrDefault(e => e.Name.LocalName == "PE_DKZ02");
            var name = peDkz02?.Elements().FirstOrDefault(e => e.Name.LocalName == "NAME_FORMATIERT")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var birthDate = ParseCompactDate(peDkz02?.Elements().FirstOrDefault(e => e.Name.LocalName == "GEBURTSDATUM")?.Value);

            var pnr = NormalizePnr(AttributeOrElementValue(per, "PNR"));

            people.Add(new FirmenbuchPerson
            {
                Name = name.Trim(),
                BirthDate = birthDate,
                Role = pnr != null ? roleByPnr.GetValueOrDefault(pnr) : null
            });
        }

        return new FirmenbuchAuszug
        {
            Euid = euid,
            FoundedDate = foundedDate,
            People = people
        };
    }

    /// <summary>
    /// Firmenbuchnummern kommen teils mit "FN"-Praefix oder Leerzeichen vor dem
    /// Pruefzeichen ("FN 120121 z") - die Schnittstelle erwartet nur Ziffern+Pruefzeichen
    /// und lehnt alles andere mit einem SOAP-Fault ab.
    /// </summary>
    internal static string NormalizeFnr(string fnr)
    {
        var trimmed = fnr.Trim();
        if (trimmed.StartsWith("FN", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];
        return string.Concat(trimmed.Where(c => !char.IsWhiteSpace(c)));
    }

    private static string? AttributeOrElementValue(XElement element, string localName) =>
        element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value
        ?? element.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;

    private static string? NormalizePnr(string? pnr) => string.IsNullOrWhiteSpace(pnr) ? null : pnr.Trim();

    private static DateOnly? ParseDate(string? value) =>
        !string.IsNullOrEmpty(value) && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static DateOnly? ParseCompactDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
