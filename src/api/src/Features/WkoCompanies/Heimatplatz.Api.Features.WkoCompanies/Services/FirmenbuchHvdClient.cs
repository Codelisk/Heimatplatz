using System.Globalization;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Heimatplatz.Api.Features.WkoCompanies.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.WkoCompanies.Services;

/// <summary>
/// Client fuer die amtliche "FBW-WebServices (HVD)"-Schnittstelle des Bundesministeriums fuer
/// Justiz (SOAP 1.2, X-API-KEY-Header). Reichert WkoCompany-Datensaetze ueber die
/// Firmenbuchnummer (FNR) mit amtlichen Daten an: praezises Gruendungsdatum (frühestes
/// Vollzugsdatum), EUID und Geschaeftsfuehrung samt Geburtsdatum. Kein WSDL/SOAP-Toolchain
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
                    new XElement(aus + "FNR", fnr),
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

    private static FirmenbuchAuszug ParseAuszug(XElement auszug)
    {
        var euidContainer = auszug.Elements().FirstOrDefault(e => e.Name.LocalName == "EUID");
        var euid = euidContainer?.Elements().FirstOrDefault(e => e.Name.LocalName == "EUID")?.Value;

        var vollzugsDates = auszug.Elements()
            .Where(e => e.Name.LocalName == "VOLLZ")
            .Select(e => e.Elements().FirstOrDefault(c => c.Name.LocalName == "VOLLZUGSDATUM")?.Value)
            .Select(ParseDate)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        DateOnly? foundedDate = vollzugsDates.Count > 0 ? vollzugsDates.Min() : null;

        // Rollentext je Personennummer (PNR, z.B. "  A") aus den FUN-Eintraegen sammeln,
        // um ihn anschliessend den PER-Eintraegen (selbe PNR) zuzuordnen.
        var roleByPnr = new Dictionary<string, string>();
        foreach (var fun in auszug.Elements().Where(e => e.Name.LocalName == "FUN"))
        {
            var pnr = NormalizePnr(fun.Elements().FirstOrDefault(e => e.Name.LocalName == "PNR")?.Value);
            var roleText = fun.Elements().FirstOrDefault(e => e.Name.LocalName == "FKENTEXT")?.Value;
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

            var birthText = peDkz02?.Elements().FirstOrDefault(e => e.Name.LocalName == "GEBURTSDATUM")?.Value;
            var birthDate = DateOnly.TryParseExact(birthText, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birth)
                ? birth
                : (DateOnly?)null;

            var pnr = NormalizePnr(per.Elements().FirstOrDefault(e => e.Name.LocalName == "PNR")?.Value);

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

    private static string? NormalizePnr(string? pnr) => string.IsNullOrWhiteSpace(pnr) ? null : pnr.Trim();

    private static DateOnly? ParseDate(string? value) =>
        !string.IsNullOrEmpty(value) && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
