using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Services;

/// <summary>
/// Scraper fuer edikte.justiz.gv.at - parst HTML der Zwangsversteigerungs-Seiten
/// </summary>
public partial class EdikteScraper(
    HttpClient httpClient,
    IOptions<ScrapingOptions> options,
    ILogger<EdikteScraper> logger
) : IEdikteScraper
{
    // URL-Pattern: Detail-Seite eines Edikts
    private const string DetailUrlTemplate =
        "/edikte/ex/exedi3.nsf/alldoc/{0}!OpenDocument";

    private string BuildSearchUrl()
    {
        var blCode = options.Value.BundeslandCode;
        if (blCode.HasValue)
        {
            // Filter nach Bundesland: [BL]=(code)
            return $"/edikte/ex/exedi3.nsf/suchedi?SearchView&subf=eex&SearchOrder=4&SearchMax=4999&query=%28%5BBL%5D%3D%28{blCode.Value}%29%29";
        }

        // Alle Bundeslaender: [VKat]>0
        return "/edikte/ex/exedi3.nsf/suchedi?SearchView&subf=eex&SearchOrder=4&SearchMax=4999&Query=%5BVKat%5D%3E0";
    }

    public async Task<List<EdiktListItem>> GetAuctionListAsync(CancellationToken ct = default)
    {
        var url = $"{options.Value.BaseUrl}{BuildSearchUrl()}";
        logger.LogInformation("Scraping Edikte-Ergebnisliste von {Url}", url);

        var html = await httpClient.GetStringAsync(url, ct);
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), ct);

        var items = new List<EdiktListItem>();
        var rows = document.QuerySelectorAll("table tbody tr");

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("td");
            if (cells.Length < 4) continue;

            // Spalte 2: Link mit Status und Datum
            var link = cells[1].QuerySelector("a");
            if (link == null) continue;

            var href = link.GetAttribute("href") ?? "";
            var externalId = ExtractExternalId(href);
            if (string.IsNullOrEmpty(externalId)) continue;

            var statusAndDate = link.TextContent.Trim();

            // Spalte 3: Adresse und Kategorie
            var addressCell = cells[2];
            var addressTexts = addressCell.ChildNodes
                .Where(n => n.NodeType == NodeType.Text)
                .Select(n => n.TextContent.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            string? postalCode = null;
            string? city = null;
            string? address = null;
            string? categoryText = null;

            // Erster Text: "PLZ Ort Strasse"
            if (addressTexts.Count > 0)
            {
                var firstLine = addressTexts[0];
                var plzMatch = PlzPattern().Match(firstLine);
                if (plzMatch.Success)
                {
                    postalCode = plzMatch.Groups[1].Value;
                    var rest = firstLine[plzMatch.Length..].Trim();
                    // Versuche Ort und Strasse zu trennen
                    address = rest;
                    var parts = rest.Split(' ', 2);
                    if (parts.Length >= 1)
                        city = parts[0];
                }
            }

            // Letzter Text oder separater Node: Kategorie
            if (addressTexts.Count > 1)
                categoryText = addressTexts[^1];

            // Spalte 4: Objektbezeichnung
            var objectDescription = cells[3].TextContent.Trim();

            items.Add(new EdiktListItem
            {
                ExternalId = externalId,
                DetailUrl = href,
                StatusText = statusAndDate,
                Address = address,
                PostalCode = postalCode,
                City = city,
                CategoryText = categoryText,
                ObjectDescription = objectDescription
            });
        }

        logger.LogInformation("{Count} Edikte in der Ergebnisliste gefunden", items.Count);
        return items;
    }

    public async Task<EdiktDetail> GetAuctionDetailAsync(string externalId, CancellationToken ct = default)
    {
        var url = $"{options.Value.BaseUrl}{string.Format(DetailUrlTemplate, externalId)}";

        // Rate-Limiting
        await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);

        logger.LogDebug("Scraping Edikt-Detail {ExternalId} von {Url}", externalId, url);

        var html = await httpClient.GetStringAsync(url, ct);
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), ct);

        var allFields = new Dictionary<string, string>();

        // Alle div.row Elemente parsen (Label: span.col-sm-3, Wert: p.col-sm-9)
        var rows = document.QuerySelectorAll("div.row");
        foreach (var row in rows)
        {
            var label = row.QuerySelector("span.col-sm-3")?.TextContent.Trim().TrimEnd(':');
            var value = row.QuerySelector("p.col-sm-9")?.TextContent.Trim();
            if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(value))
                allFields[label] = value;
        }

        // Titel (h1 small)
        var title = document.QuerySelector(".page-header h1 small")?.TextContent.Trim();

        // Dokument-Links extrahieren
        string? shortAppraisalUrl = null;
        string? longAppraisalUrl = null;
        string? sitePlanUrl = null;
        string? floorPlanUrl = null;

        var strongElements = document.QuerySelectorAll("strong");
        foreach (var strong in strongElements)
        {
            var text = strong.TextContent.Trim().TrimEnd(':');
            var parent = strong.ParentElement;
            var nextSibling = parent?.NextElementSibling;
            var link = nextSibling?.QuerySelector("a") ?? parent?.QuerySelector("a");
            var href = link?.GetAttribute("href");

            if (string.IsNullOrEmpty(href)) continue;

            var fullUrl = href.StartsWith("http") ? href : $"{options.Value.BaseUrl}{href}";

            switch (text)
            {
                case "Kurzgutachten":
                    shortAppraisalUrl = fullUrl;
                    break;
                case "Langgutachten":
                    longAppraisalUrl = fullUrl;
                    break;
                case "Lageplan":
                    sitePlanUrl = fullUrl;
                    break;
                case "Grundriss(e)":
                    floorPlanUrl = fullUrl;
                    break;
            }
        }

        // Alle Bild-Attachments extrahieren (JPG/PNG Links, keine Thumbnails)
        var imageUrls = new List<string>();
        var allLinks = document.QuerySelectorAll("a[href]");
        foreach (var link in allLinks)
        {
            var href = link.GetAttribute("href");
            if (string.IsNullOrEmpty(href)) continue;

            // Nur Bild-Dateien, keine Thumbnails (th1...)
            var lowerHref = href.ToLowerInvariant();
            if ((lowerHref.EndsWith(".jpg") || lowerHref.EndsWith(".jpeg") || lowerHref.EndsWith(".png"))
                && !lowerHref.Contains("/th1"))
            {
                var fullImgUrl = href.StartsWith("http") ? href : $"{options.Value.BaseUrl}{href}";
                if (!imageUrls.Contains(fullImgUrl))
                    imageUrls.Add(fullImgUrl);
            }
        }

        // Publikations-Eintraege am Ende
        var publications = document.QuerySelectorAll("#druckbereich > div:last-of-type p");
        string? publicationDate = null;
        string? statusFromPublications = null;
        if (publications.Length >= 2)
        {
            publicationDate = publications[0].TextContent.Trim();
            statusFromPublications = publications[1].TextContent.Trim();
        }

        return new EdiktDetail
        {
            ExternalId = externalId,
            Court = allFields.GetValueOrDefault("Dienststelle"),
            CaseNumber = allFields.GetValueOrDefault("Aktenzeichen"),
            Reason = allFields.GetValueOrDefault("wegen"),
            AuctionDateText = allFields.GetValueOrDefault("Termin")
                ?? allFields.GetValueOrDefault("Neuer Versteigerungstermin")
                ?? allFields.GetValueOrDefault("Versteigerungstermin"),
            AuctionLocation = allFields.GetValueOrDefault("Ort")
                ?? allFields.GetValueOrDefault("Neuer Ort")
                ?? allFields.GetValueOrDefault("Versteigerungsort"),
            CadastralMunicipality = allFields.GetValueOrDefault("Grundbuch"),
            RegistrationNumber = allFields.GetValueOrDefault("EZ"),
            PlotNumber = allFields.GetValueOrDefault("Grundstücksnr."),
            SheetNumber = allFields.GetValueOrDefault("BLNr"),
            Address = allFields.GetValueOrDefault("Liegenschaftsadresse"),
            PostalCodeAndCity = allFields.GetValueOrDefault("PLZ/Ort"),
            CategoryText = allFields.GetValueOrDefault("Kategorie(n)"),
            ObjectDescription = allFields.GetValueOrDefault("Beschreibung (WE)")
                ?? allFields.GetValueOrDefault("Beschreibung (EFH)")
                ?? allFields.GetValueOrDefault("Beschreibung (Gew.)")
                ?? allFields.GetValueOrDefault("Beschreibung")
                ?? title,
            PlotAreaText = allFields.GetValueOrDefault("Grundstücksgröße"),
            ObjectAreaText = allFields.GetValueOrDefault("Objektgröße"),
            EstimatedValueText = allFields.GetValueOrDefault("Schätzwert"),
            MinimumBidText = allFields.GetValueOrDefault("Geringstes Gebot"),
            VadiumText = allFields.GetValueOrDefault("Vadium"),
            ShortAppraisalUrl = shortAppraisalUrl,
            LongAppraisalUrl = longAppraisalUrl,
            SitePlanUrl = sitePlanUrl,
            FloorPlanUrl = floorPlanUrl,
            ImageUrls = imageUrls,
            StatusText = statusFromPublications ?? title,
            LastChangeDateText = allFields.GetValueOrDefault("Letzte Änderung am"),
            PublicationDateText = publicationDate,
            AllFields = allFields
        };
    }

    private static string? ExtractExternalId(string href)
    {
        // URL-Pattern: alldoc/{documentId}!OpenDocument
        var match = ExternalIdPattern().Match(href);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"alldoc/([a-f0-9]+)!OpenDocument", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalIdPattern();

    [GeneratedRegex(@"^(\d{4})\s")]
    private static partial Regex PlzPattern();
}
