using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Heimatplatz.Api.Features.WkoCompanies.Configuration;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.WkoCompanies.Services;

/// <summary>
/// Scraper fuer firmen.wko.at (WKO Firmen-A-Z). Die Seite ist klassisches ASP.NET WebForms mit
/// AJAX-UpdatePanel-Pagination ("Mehr laden" postet den kompletten ViewState zurueck und erhaelt
/// eine laengenpraefixierte Delta-Antwort, siehe AspNetAjaxDeltaParser) statt einem einfachen
/// Query-Parameter wie bei der Ediktsdatei.
/// </summary>
public partial class WkoCompanyScraper(
    HttpClient httpClient,
    IOptions<WkoScrapingOptions> options,
    ILogger<WkoCompanyScraper> logger
) : IWkoCompanyScraper
{
    // Fest verdrahtet statt aus dem initialen HTML gelesen: dieses Feld liefert der Server nicht
    // als statisches Hidden-Input (der ScriptManager traegt es erst beim Postback per Client-JS
    // ein) - der Wert wurde per Browser-Netzwerkmitschnitt der echten "Mehr laden"-Aktion ermittelt
    // und ist stabil, solange die Panel-/Button-IDs auf firmen.wko.at unveraendert bleiben.
    private const string AjaxScriptManagerValue =
        "ctl00$ContentPlaceHolder1$resultsUpdatePanel|ctl00$ContentPlaceHolder1$nextPageButton";
    private const string NextPageButtonFieldName = "ctl00$ContentPlaceHolder1$nextPageButton";
    private const string ResultsPanelIdSuffix = "resultsUpdatePanel";
    private const string NextPageButtonElementId = "ctl00_ContentPlaceHolder1_nextPageButton";

    public async Task<List<WkoCompanySearchResult>> SearchAsync(string keyword, string region, CancellationToken ct = default)
    {
        var url = $"{options.Value.BaseUrl}/{Uri.EscapeDataString(keyword)}/{Uri.EscapeDataString(region)}";
        logger.LogInformation("WKO-Suche nach '{Keyword}' in '{Region}': {Url}", keyword, region, url);

        var html = await httpClient.GetStringAsync(url, ct);
        var document = await ParseHtmlAsync(html, ct);

        var formFields = ExtractFormFields(document);
        var results = ParseSearchResultCards(document, options.Value.BaseUrl);
        var hasNextPage = document.GetElementById(NextPageButtonElementId) != null;

        var page = 1;
        while (hasNextPage && page < options.Value.MaxPagesPerKeyword)
        {
            await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);
            page++;

            formFields["__EVENTTARGET"] = "";
            formFields["__EVENTARGUMENT"] = "";
            formFields["__ASYNCPOST"] = "true";
            formFields["ctl00$ajaxScriptManager"] = AjaxScriptManagerValue;
            formFields[NextPageButtonFieldName] = "Mehr laden";

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formFields)
            };
            request.Headers.Referrer = new Uri(url);

            using var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var deltaResponse = await response.Content.ReadAsStringAsync(ct);

            var blocks = AspNetAjaxDeltaParser.Parse(deltaResponse);

            foreach (var field in blocks.Where(b => b.Type == "hiddenField"))
                formFields[field.Id] = field.Content;

            var panel = blocks.FirstOrDefault(b => b.Type == "updatePanel" && b.Id.EndsWith(ResultsPanelIdSuffix, StringComparison.Ordinal));
            if (panel == null)
            {
                logger.LogWarning("WKO-Postback fuer '{Keyword}' Seite {Page}: kein resultsUpdatePanel in der Antwort - breche Pagination ab", keyword, page);
                break;
            }

            var panelDocument = await ParseHtmlAsync(panel.Content, ct);
            var panelResults = ParseSearchResultCards(panelDocument, options.Value.BaseUrl);

            // Das Panel liefert bei jedem "Mehr laden" den KUMULATIVEN Ergebnis-Stand (nicht nur die
            // neuen Treffer) - ersetzen statt anhaengen vermeidet Duplikate innerhalb dieses Laufs.
            if (panelResults.Count <= results.Count)
            {
                logger.LogInformation("WKO-Suche '{Keyword}': Seite {Page} brachte keine neuen Treffer - Pagination beendet", keyword, page);
                break;
            }

            results = panelResults;
            hasNextPage = panelDocument.GetElementById(NextPageButtonElementId) != null;
        }

        if (page >= options.Value.MaxPagesPerKeyword && hasNextPage)
        {
            logger.LogWarning("WKO-Suche '{Keyword}': Sicherheitsgrenze von {MaxPages} Seiten erreicht, moeglicherweise nicht alle Treffer geladen", keyword, options.Value.MaxPagesPerKeyword);
        }

        logger.LogInformation("WKO-Suche '{Keyword}' in '{Region}': {Count} Firmen gefunden", keyword, region, results.Count);
        return results;
    }

    public async Task<WkoCompanyDetail> GetDetailAsync(string detailUrl, CancellationToken ct = default)
    {
        var url = ToAbsoluteUrl(detailUrl, options.Value.BaseUrl);

        // Rate-Limiting
        await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);

        logger.LogDebug("Scraping WKO-Detailseite {Url}", url);

        var html = await httpClient.GetStringAsync(url, ct);
        var document = await ParseHtmlAsync(html, ct);

        var name = document.QuerySelector("h1.detail-heading")?.TextContent.Trim();
        var categoryText = document.QuerySelector("h1.detail-heading")?.NextElementSibling?.TextContent.Trim();

        var (street, postalCode, city) = ExtractAddress(document);
        var phones = ExtractItemPropValues(document, "telephone");
        var email = ExtractItemPropValues(document, "email").FirstOrDefault();
        var website = ExtractItemPropValues(document, "url").FirstOrDefault();

        var openingHours = document.QuerySelector("p.info-text")?.TextContent.Trim();
        if (openingHours != null)
            openingHours = OpeningHoursPrefixPattern().Replace(openingHours, "").Trim();

        int? foundedYear = null;
        var aboutHeading = document.QuerySelectorAll("h3").FirstOrDefault(h => h.TextContent.Trim() == "Über uns");
        var aboutText = aboutHeading?.NextElementSibling?.TextContent.Trim();
        if (aboutText != null)
        {
            var yearMatch = LeadingYearPattern().Match(aboutText);
            if (yearMatch.Success)
                foundedYear = int.Parse(yearMatch.Groups[1].Value);
        }

        var firmendaten = ParseDtDdPairs(document.QuerySelector("#firmendaten"));

        var permits = document
            .QuerySelectorAll("#berechtigungen .card.card-content")
            .Select(card =>
            {
                var hiddenFields = ParseDtDdPairs(card.QuerySelector("div[hidden]"));
                return new WkoCompanyPermit
                {
                    FachgruppeName = card.QuerySelector(".card-heading")?.TextContent.Trim(),
                    Description = hiddenFields.GetValueOrDefault("Gewerbewortlaut"),
                    ManagingDirector = hiddenFields.GetValueOrDefault("Gewerberechtliche Geschäftsführung")?.Trim(),
                    GisaNumber = hiddenFields.GetValueOrDefault("GISA-Zahl"),
                    // "Datum" im versteckten Detail-Block: "Seit 28.10.2016 (kann vom Gruendungsdatum abweichen)"
                    Since = ParseGermanDate(hiddenFields.GetValueOrDefault("Datum"))
                };
            })
            .ToList();

        return new WkoCompanyDetail
        {
            Name = name,
            CategoryText = categoryText ?? firmendaten.GetValueOrDefault("Geschäftsbezeichnung"),
            Street = street,
            PostalCode = postalCode,
            City = city,
            Phones = phones,
            Email = email,
            Website = website,
            OpeningHoursText = openingHours,
            CompanyRegisterNumber = firmendaten.GetValueOrDefault("Firmenbuchnummer"),
            CompanyCourt = firmendaten.GetValueOrDefault("Firmengericht"),
            Gln = firmendaten.GetValueOrDefault("GLN"),
            LegalForm = firmendaten.GetValueOrDefault("Rechtsform"),
            FoundedYear = foundedYear,
            Permits = permits
        };
    }

    private static async Task<IDocument> ParseHtmlAsync(string html, CancellationToken ct)
    {
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        return await context.OpenAsync(req => req.Content(html), ct);
    }

    /// <summary>
    /// Sammelt alle benannten Formularfelder (nicht nur Hidden-Inputs) wie es ein echter Browser
    /// beim Postback mitschicken wuerde - firmen.wko.at postet z.B. auch das sichtbare
    /// Suchbegriff-/Standort-Textfeld mit (txtSuchbegriff/txtStandort). Fehlen diese, ist nicht
    /// sicher, dass der Server "Mehr laden" im Kontext der aktuellen Suche verarbeitet.
    /// </summary>
    private static Dictionary<string, string> ExtractFormFields(IDocument document)
    {
        var fields = new Dictionary<string, string>();

        foreach (var input in document.QuerySelectorAll("input"))
        {
            var name = input.GetAttribute("name");
            if (string.IsNullOrEmpty(name)) continue;

            var type = (input.GetAttribute("type") ?? "text").ToLowerInvariant();
            if (type is "submit" or "button" or "image" or "reset" or "file") continue;
            if (type is "checkbox" or "radio" && input.GetAttribute("checked") == null) continue;

            fields[name] = input.GetAttribute("value") ?? "";
        }

        foreach (var select in document.QuerySelectorAll("select"))
        {
            var name = select.GetAttribute("name");
            if (string.IsNullOrEmpty(name)) continue;

            var selected = select.QuerySelector("option[selected]") ?? select.QuerySelector("option");
            fields[name] = selected?.GetAttribute("value") ?? selected?.TextContent.Trim() ?? "";
        }

        foreach (var textarea in document.QuerySelectorAll("textarea"))
        {
            var name = textarea.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
                fields[name] = textarea.TextContent;
        }

        return fields;
    }

    private static List<WkoCompanySearchResult> ParseSearchResultCards(IParentNode container, string baseUrl)
    {
        var results = new List<WkoCompanySearchResult>();

        foreach (var article in container.QuerySelectorAll("article.search-result-article"))
        {
            var link = article.QuerySelector("a.title-link, .search-result-header a");
            var href = link?.GetAttribute("href");
            var firmaId = ExtractFirmaId(href);
            var name = link?.QuerySelector("h3")?.TextContent.Trim();

            if (firmaId == null || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(href))
                continue;

            var (street, postalCode, city) = ExtractAddress(article);
            var isTrainingCompany = article
                .QuerySelectorAll("div, span")
                .Any(e => e.Children.Length == 0 && e.TextContent.Trim() == "Lehrbetrieb");

            results.Add(new WkoCompanySearchResult
            {
                WkoFirmaId = firmaId.Value,
                Name = name,
                DetailUrl = ToAbsoluteUrl(href, baseUrl),
                CategoryText = article.QuerySelector(".title-details")?.TextContent.Trim(),
                Street = street,
                PostalCode = postalCode,
                City = city,
                Phones = ExtractItemPropValues(article, "telephone"),
                Email = ExtractItemPropValues(article, "email").FirstOrDefault(),
                Website = ExtractItemPropValues(article, "url").FirstOrDefault(),
                IsTrainingCompany = isTrainingCompany
            });
        }

        return results;
    }

    /// <summary>
    /// Adresse extrahieren. Die Detailseite liefert PLZ/Ort ueber schema.org-Microdata
    /// (itemprop=postalCode/addressLocality), die Trefferlisten-Karte nur als ein kombiniertes
    /// "&lt;PLZ&gt;&amp;nbsp;&lt;Ort&gt;"-Textfeld (.place) - beide Faelle abdecken.
    /// </summary>
    private static (string? Street, string? PostalCode, string? City) ExtractAddress(IParentNode container)
    {
        var street = container.QuerySelector(".street")?.TextContent.Trim();
        var postalCode = container.QuerySelector("[itemprop=postalCode]")?.TextContent.Trim();
        var city = container.QuerySelector("[itemprop=addressLocality]")?.TextContent.Trim();

        if (postalCode == null || city == null)
        {
            var placeText = container.QuerySelector(".place")?.TextContent.Trim();
            if (!string.IsNullOrEmpty(placeText))
            {
                var match = PlaceTextPattern().Match(placeText);
                if (match.Success)
                {
                    postalCode ??= match.Groups[1].Value;
                    city ??= match.Groups[2].Value.Trim();
                }
            }
        }

        return (street, postalCode, city);
    }

    private static List<string> ExtractItemPropValues(IParentNode container, string itemProp) =>
        container.QuerySelectorAll($"[itemprop={itemProp}]")
            .Select(e => e.TextContent.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

    /// <summary>
    /// Paart dt/dd-Elemente eines Containers (Firmendaten-Abschnitt bzw. das versteckte
    /// Berechtigungs-Detail-div) zu einem Dictionary. Fuer dd-Werte mit einer .number-Span
    /// (z.B. GLN, wo daneben noch ein "In die Zwischenablage kopieren"-Link steckt) wird gezielt
    /// nur die Nummer gelesen statt des gesamten dd-Textinhalts.
    /// </summary>
    private static Dictionary<string, string> ParseDtDdPairs(IElement? container)
    {
        var result = new Dictionary<string, string>();
        if (container == null) return result;

        foreach (var dt in container.QuerySelectorAll("dt"))
        {
            var dd = dt.NextElementSibling;
            if (dd == null || !dd.NodeName.Equals("dd", StringComparison.OrdinalIgnoreCase))
                continue;

            var key = dt.TextContent.Trim().TrimEnd(':');
            var value = dd.QuerySelector(".number")?.TextContent.Trim() ?? dd.TextContent.Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result[key] = value;
        }

        return result;
    }

    // WKO nennt nur einen Kalendertag, keine Uhrzeit. Auf Wiener MITTAG verankern statt
    // Mitternacht (Mitternacht Wien = 22:00 UTC des Vortags), damit der Kalendertag in
    // jeder Anzeige-Zeitzone stabil bleibt - gleiches Muster wie ParsePublicationDate
    // im ForeclosureAuctionSyncService.
    private static readonly TimeZoneInfo ViennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    private static DateTimeOffset? ParseGermanDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = GermanDatePattern().Match(text);
        if (!match.Success) return null;

        if (!DateTime.TryParseExact(
            match.Groups[1].Value,
            "d.M.yyyy", // d/M matchen beim Parsen ein- UND zweistellig
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDate))
        {
            return null;
        }

        var viennaNoon = localDate.Date.AddHours(12);
        var utc = TimeZoneInfo.ConvertTimeToUtc(viennaNoon, ViennaTimeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static Guid? ExtractFirmaId(string? href)
    {
        if (string.IsNullOrEmpty(href)) return null;
        var match = FirmaIdPattern().Match(href);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }

    private static string ToAbsoluteUrl(string url, string baseUrl)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), url.TrimStart('/')).ToString();
    }

    [GeneratedRegex(@"firmaid=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})")]
    private static partial Regex FirmaIdPattern();

    [GeneratedRegex(@"^(\d{4})[\s ]+(.+)$")]
    private static partial Regex PlaceTextPattern();

    [GeneratedRegex(@"^(\d{4})")]
    private static partial Regex LeadingYearPattern();

    [GeneratedRegex(@"^Öffnungszeiten:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex OpeningHoursPrefixPattern();

    [GeneratedRegex(@"(\d{1,2}\.\d{1,2}\.\d{4})")]
    private static partial Regex GermanDatePattern();
}
