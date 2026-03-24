using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using Heimatplatz.Api.Features.SrealListings.Configuration;
using Heimatplatz.Api.Features.SrealListings.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimatplatz.Api.Features.SrealListings.Services;

/// <summary>
/// Scraper fuer sreal.at - parst HTML der Immobilien-Suchergebnisse und Detailseiten
/// </summary>
public partial class SrealScraper(
    HttpClient httpClient,
    IOptions<SrealScrapingOptions> options,
    ILogger<SrealScraper> logger
) : ISrealScraper
{
    public async Task<List<SrealListItem>> GetListingsAsync(CancellationToken ct = default)
    {
        var allItems = new List<SrealListItem>();
        var page = 1;
        var hasMorePages = true;

        while (hasMorePages)
        {
            var url = BuildSearchUrl(page);
            logger.LogInformation("Scraping sreal.at Seite {Page} von {Url}", page, url);

            var html = await httpClient.GetStringAsync(url, ct);
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html), ct);

            var items = ParseListPage(document);
            if (items.Count == 0)
            {
                hasMorePages = false;
                continue;
            }

            allItems.AddRange(items);

            // Pruefen ob es eine naechste Seite gibt
            hasMorePages = HasNextPage(document, page);
            page++;

            if (page > 20) // Safety-Limit
            {
                logger.LogWarning("Safety-Limit von 20 Seiten erreicht");
                break;
            }

            // Rate-Limiting zwischen Seiten
            await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);
        }

        logger.LogInformation("{Count} Inserate auf {Pages} Seiten gefunden", allItems.Count, page - 1);
        return allItems;
    }

    public async Task<SrealDetail> GetListingDetailAsync(string relativeUrl, CancellationToken ct = default)
    {
        var url = relativeUrl.StartsWith("http")
            ? relativeUrl
            : $"{options.Value.BaseUrl}{relativeUrl}";

        // Rate-Limiting
        await Task.Delay(options.Value.DelayBetweenRequestsMs, ct);

        logger.LogDebug("Scraping sreal.at Detail von {Url}", url);

        var html = await httpClient.GetStringAsync(url, ct);
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), ct);

        return ParseDetailPage(document, relativeUrl, url);
    }

    private string BuildSearchUrl(int page)
    {
        var opts = options.Value;
        var queryParams = HttpUtility.ParseQueryString(string.Empty);

        queryParams["f[buyingType]"] = opts.BuyingType;

        for (var i = 0; i < opts.Locations.Count; i++)
            queryParams[$"f[location_or_id][{i}]"] = opts.Locations[i];

        for (var i = 0; i < opts.ObjectTypes.Count; i++)
            queryParams[$"f[objectType][{i}]"] = opts.ObjectTypes[i];

        queryParams["f[sorting]"] = opts.Sorting;

        if (page > 1)
            queryParams["p"] = page.ToString();

        return $"{opts.BaseUrl}{opts.SearchPath}?{queryParams}";
    }

    private List<SrealListItem> ParseListPage(IDocument document)
    {
        var items = new List<SrealListItem>();

        // sreal.at listing links: <a> elements with href matching /de/immobilie/{id}/{slug}
        var listingLinks = document.QuerySelectorAll("a[href]")
            .Where(a =>
            {
                var href = a.GetAttribute("href") ?? "";
                return ImmobilieUrlPattern().IsMatch(href);
            })
            .ToList();

        // Deduplicate by href (same listing may appear multiple times)
        var seenUrls = new HashSet<string>();

        foreach (var link in listingLinks)
        {
            var href = link.GetAttribute("href") ?? "";
            if (!seenUrls.Add(href)) continue;

            var externalId = ExtractExternalId(href);
            if (string.IsNullOrEmpty(externalId)) continue;

            // Extract data from the listing card
            var title = link.QuerySelector("h3")?.TextContent.Trim()
                ?? link.QuerySelector("h2")?.TextContent.Trim()
                ?? "";
            if (string.IsNullOrEmpty(title)) continue;

            // Location text (first <p> or text with PLZ pattern)
            string? address = null;
            string? postalCode = null;
            string? city = null;
            string? areaText = null;
            string? priceText = null;

            var paragraphs = link.QuerySelectorAll("p, span, div")
                .Select(e => e.TextContent.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            foreach (var text in paragraphs)
            {
                var plzMatch = PlzPattern().Match(text);
                if (plzMatch.Success && postalCode == null)
                {
                    postalCode = plzMatch.Groups[1].Value;
                    city = StripObjectIdSuffix(text[plzMatch.Length..].Trim());
                    address = $"{postalCode} {city}";
                }
                else if (text.Contains("m²") || text.Contains("m2"))
                {
                    areaText = text;
                }
                else if (text.Contains("€") || text.Contains("EUR"))
                {
                    priceText = text;
                }
            }

            // Image URL
            var img = link.QuerySelector("img");
            var imageUrl = img?.GetAttribute("src") ?? img?.GetAttribute("data-src");

            // Determine object type from URL slug or context
            var objectType = GuessObjectTypeFromUrl(href, title);

            items.Add(new SrealListItem
            {
                ExternalId = externalId,
                DetailUrl = href,
                Title = title,
                Address = address,
                PostalCode = postalCode,
                City = city,
                AreaText = areaText,
                PriceText = priceText,
                ImageUrl = imageUrl,
                ObjectType = objectType
            });
        }

        return items;
    }

    private static bool HasNextPage(IDocument document, int currentPage)
    {
        // Look for pagination links with page number > current
        var paginationLinks = document.QuerySelectorAll("a[href]")
            .Where(a =>
            {
                var href = a.GetAttribute("href") ?? "";
                return href.Contains($"p={currentPage + 1}");
            });

        return paginationLinks.Any();
    }

    private SrealDetail ParseDetailPage(IDocument document, string relativeUrl, string fullUrl)
    {
        var externalId = ExtractExternalId(relativeUrl) ?? "";
        var allFields = new Dictionary<string, string>();

        // Title: first h2 in main content (not footer etc.)
        var title = document.QuerySelector("main h2")?.TextContent.Trim()
            ?? document.QuerySelector("h2")?.TextContent.Trim()
            ?? "";
        allFields["Title"] = title;

        // Extract data from mtmDataLayer script
        var scriptData = ExtractMtmDataLayer(document);

        // === Address: <span> with PLZ pattern, format "4121 Altenfelden - 964/31511" ===
        string? addressText = null;
        string? postalCode = null;
        string? city = null;

        // Strategy 1: Find any span in the page with PLZ pattern (most reliable)
        foreach (var span in document.QuerySelectorAll("span"))
        {
            var text = span.TextContent.Trim();
            if (text.Length > 100 || text.Length < 5) continue;
            var plzMatch = PlzPattern().Match(text);
            if (!plzMatch.Success) continue;

            postalCode = plzMatch.Groups[1].Value;
            city = StripObjectIdSuffix(text[plzMatch.Length..].Trim());
            addressText = $"{postalCode} {city}";
            break;
        }

        // Strategy 2: h2 sibling traversal (if span approach failed)
        if (postalCode == null)
        {
            var h2 = document.QuerySelector("h2");
            if (h2?.ParentElement != null)
            {
                foreach (var node in h2.ParentElement.ChildNodes)
                {
                    var text = node.TextContent.Trim();
                    if (text.Length > 100 || text.Length < 5) continue;
                    var plzMatch = PlzPattern().Match(text);
                    if (!plzMatch.Success) continue;

                    postalCode = plzMatch.Groups[1].Value;
                    city = StripObjectIdSuffix(text[plzMatch.Length..].Trim());
                    addressText = $"{postalCode} {city}";
                    break;
                }
            }
        }

        // Strategy 3: mtmDataLayer fallback
        if (postalCode == null && scriptData.TryGetValue("zip", out var zip))
            postalCode = zip;
        if (city == null && scriptData.TryGetValue("city", out var cityVal))
            city = cityVal;
        addressText ??= $"{postalCode} {city}".Trim();

        allFields["Address"] = addressText ?? "";
        allFields["PostalCode"] = postalCode ?? "";
        allFields["City"] = city ?? "";

        // === District from breadcrumbs: links in main before the h2 ===
        // Pattern: Haus > Oberösterreich > Rohrbach > Kauf (Rohrbach is the district)
        var district = ExtractDistrict(document);
        allFields["District"] = district ?? "";

        // === Price & Commission from structured list under "Preise" heading ===
        string? priceText = null;
        decimal? price = null;
        string? commission = null;
        var priceCommPairs = ParseLabelValuePairs(document, "Preise");
        if (priceCommPairs.TryGetValue("Kaufpreis", out var kp))
        {
            priceText = kp;
            price = ParsePrice(kp);
        }
        if (priceCommPairs.TryGetValue("Provision", out var prov))
            commission = prov;

        // Fallback: mtmDataLayer
        if (price == null && scriptData.TryGetValue("price", out var priceVal))
        {
            priceText = priceVal;
            price = ParsePrice(priceVal);
        }
        allFields["Price"] = priceText ?? "";
        allFields["Commission"] = commission ?? "";

        // === Areas from structured data or mtmDataLayer ===
        decimal? livingArea = null;
        decimal? plotArea = null;
        int? rooms = null;

        if (scriptData.TryGetValue("area", out var areaVal) && areaVal != "N/A")
            livingArea = ParseArea(areaVal);
        if (scriptData.TryGetValue("surfaceArea", out var surfaceVal) && surfaceVal != "N/A")
            plotArea = ParseArea(surfaceVal);
        if (scriptData.TryGetValue("rooms", out var roomsVal) && roomsVal != "N/A")
            rooms = int.TryParse(roomsVal, out var r) ? r : null;

        // Fallback: parse from detail list items
        livingArea ??= FindAndParseArea(document, "Wohnfläche", "Wohnflaeche");
        plotArea ??= FindAndParseArea(document, "Grundfläche", "Grundflaeche", "Grundstücksfläche");
        rooms ??= FindAndParseInt(document, "Zimmer");

        allFields["LivingArea"] = livingArea?.ToString() ?? "";
        allFields["PlotArea"] = plotArea?.ToString() ?? "";
        allFields["Rooms"] = rooms?.ToString() ?? "";

        // === Energy data from "Merkmale" section ===
        string? energyClass = null;
        string? energyValue = null;
        string? fgee = null;
        string? fgeeClass = null;

        var merkmalePairs = ParseLabelValuePairs(document, "Merkmale");
        if (merkmalePairs.TryGetValue("Heizwärmeklasse", out var hwbClass))
            energyClass = hwbClass;
        if (merkmalePairs.TryGetValue("Heizwärmebedarf", out var hwbVal))
            energyValue = hwbVal;
        if (merkmalePairs.TryGetValue("fGEE", out var fgeeVal))
            fgee = fgeeVal;
        if (merkmalePairs.TryGetValue("fGEE Klasse", out var fgeeClsVal))
            fgeeClass = fgeeClsVal;

        // Fallback from mtmDataLayer
        if (energyClass == null && scriptData.TryGetValue("heatingCategory", out var heatCat))
            energyClass = heatCat;

        allFields["EnergyClass"] = energyClass ?? "";
        allFields["EnergyValue"] = energyValue ?? "";
        allFields["FGee"] = fgee ?? "";
        allFields["FGeeClass"] = fgeeClass ?? "";

        // === Description from "Objektbeschreibung" section ===
        var description = ExtractDescription(document);
        allFields["Description"] = description ?? "";

        // === Images ===
        var imageUrls = ExtractImageUrls(document);
        allFields["ImageCount"] = imageUrls.Count.ToString();

        // === Agent contact from structured section ===
        var (agentName, agentPhone, agentEmail, agentOffice) = ExtractAgentInfo(document);
        allFields["AgentName"] = agentName ?? "";
        allFields["AgentPhone"] = agentPhone ?? "";
        allFields["AgentEmail"] = agentEmail ?? "";
        allFields["AgentOffice"] = agentOffice ?? "";

        // === Infrastructure ===
        var infrastructure = ExtractInfrastructure(document);
        allFields["Infrastructure"] = infrastructure ?? "";

        // === Object type ===
        var objectType = GuessObjectTypeFromUrl(relativeUrl, title);
        if (scriptData.TryGetValue("objectType", out var objType))
        {
            objectType = objType.ToLowerInvariant() switch
            {
                "house" or "haus" => SrealObjectType.House,
                "property" or "grundstueck" or "grundstück" => SrealObjectType.Land,
                "vacation" or "ferienimmobilie" => SrealObjectType.Vacation,
                _ => objectType
            };
        }

        return new SrealDetail
        {
            ExternalId = externalId,
            Title = title,
            SourceUrl = fullUrl,
            Address = addressText,
            PostalCode = postalCode,
            City = city,
            District = district,
            ObjectType = objectType,
            BuyingType = "buy",
            PriceText = priceText,
            Price = price,
            Commission = commission,
            LivingArea = livingArea,
            PlotArea = plotArea,
            Rooms = rooms,
            Description = description,
            EnergyClass = energyClass,
            EnergyValue = energyValue,
            FGee = fgee,
            FGeeClass = fgeeClass,
            ImageUrls = imageUrls,
            AgentName = agentName,
            AgentPhone = agentPhone,
            AgentEmail = agentEmail,
            AgentOffice = agentOffice,
            Infrastructure = infrastructure,
            AllFields = allFields
        };
    }

    // === Parsing-Hilfsmethoden ===

    private static string? ExtractExternalId(string href)
    {
        var match = ImmobilieIdPattern().Match(href);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static Dictionary<string, string> ExtractMtmDataLayer(IDocument document)
    {
        var data = new Dictionary<string, string>();

        // Find script containing mtmDataLayer or similar data object
        foreach (var script in document.QuerySelectorAll("script"))
        {
            var content = script.TextContent;
            if (!content.Contains("mtmDataLayer") && !content.Contains("dataLayer"))
                continue;

            // Extract key-value pairs using regex
            var matches = DataLayerPattern().Matches(content);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value.Trim().Trim('\'', '"');
                var value = match.Groups[2].Value.Trim().Trim('\'', '"');
                // Decode Unicode escapes
                value = value.Replace("\\u00A0", " ").Replace("\\u20AC", "€");
                data[key] = value;
            }
        }

        return data;
    }

    private static string? FindTextContaining(IDocument document, string keyword)
    {
        var allText = document.QuerySelectorAll("p, span, div, li, td, dt, dd");
        foreach (var element in allText)
        {
            var text = element.TextContent.Trim();
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase) && text.Length < 500)
                return text;
        }
        return null;
    }

    private static string? FindAddressText(IDocument document)
    {
        // Look for PLZ pattern in common address locations
        var candidates = document.QuerySelectorAll("p, span, div, address")
            .Select(e => e.TextContent.Trim())
            .Where(t => PlzPattern().IsMatch(t) && t.Length < 100);

        return candidates.FirstOrDefault();
    }

    private static string? ExtractDistrict(IDocument document)
    {
        // Breadcrumbs in main: links like /de/haeuser-rohrbach-oberoesterreich/angebot/XXX
        // Pattern: Haus > Oberösterreich > [District] > Kauf
        var breadcrumbLinks = document.QuerySelectorAll("main a[href*='/angebot/']");
        string? lastDistrict = null;
        var foundState = false;

        foreach (var crumb in breadcrumbLinks)
        {
            var text = crumb.TextContent.Trim();
            if (text.Contains("sterreich", StringComparison.OrdinalIgnoreCase))
            {
                foundState = true;
                continue;
            }
            if (foundState && text != "Kauf" && text != "Miete" && !string.IsNullOrEmpty(text) && text.Length < 50)
            {
                lastDistrict = text;
                break;
            }
        }

        return lastDistrict;
    }

    /// <summary>
    /// Parst Label-Value-Paare aus einer strukturierten Liste unter einem h3 heading.
    /// sreal.at verwendet: h3 > ul > li > div > (div[label], div[value])
    /// </summary>
    private static Dictionary<string, string> ParseLabelValuePairs(IDocument document, string headingText)
    {
        var result = new Dictionary<string, string>();

        // Find the h3 heading with the matching text
        var headings = document.QuerySelectorAll("h3");
        IElement? targetHeading = null;
        foreach (var h in headings)
        {
            if (h.TextContent.Trim().Equals(headingText, StringComparison.OrdinalIgnoreCase))
            {
                targetHeading = h;
                break;
            }
        }
        if (targetHeading == null) return result;

        // Get the next sibling list element
        var sibling = targetHeading.NextElementSibling;
        while (sibling != null)
        {
            if (sibling.TagName.Equals("UL", StringComparison.OrdinalIgnoreCase)
                || sibling.TagName.Equals("OL", StringComparison.OrdinalIgnoreCase))
            {
                // Parse each li as a label-value pair
                foreach (var li in sibling.QuerySelectorAll("li"))
                {
                    var divs = li.QuerySelectorAll("div").ToList();
                    // Deepest pair: innermost div children represent label and value
                    if (divs.Count >= 2)
                    {
                        // Find the deepest container that has exactly 2 child divs
                        var container = divs.FirstOrDefault(d =>
                            d.Children.Length == 2
                            && d.Children[0].TagName == "DIV"
                            && d.Children[1].TagName == "DIV");

                        if (container != null)
                        {
                            var label = container.Children[0].TextContent.Trim();
                            var value = container.Children[1].TextContent.Trim();
                            if (!string.IsNullOrEmpty(label))
                                result[label] = value;
                        }
                    }
                }
                break;
            }
            sibling = sibling.NextElementSibling;
        }

        return result;
    }

    private static string? ExtractPriceFromText(string text)
    {
        var match = PricePattern().Match(text);
        return match.Success ? match.Value : text;
    }

    private static decimal? FindAndParseArea(IDocument document, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var text = FindTextContaining(document, keyword);
            if (text == null) continue;

            var match = AreaPattern().Match(text);
            if (match.Success)
                return ParseArea(match.Groups[1].Value);
        }
        return null;
    }

    private static int? FindAndParseInt(IDocument document, string keyword)
    {
        var text = FindTextContaining(document, keyword);
        if (text == null) return null;

        var match = IntPattern().Match(text);
        return match.Success && int.TryParse(match.Groups[1].Value, out var val) ? val : null;
    }

    private static string? FindTextValue(IDocument document, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var elements = document.QuerySelectorAll("dt, th, td, span, div, p, li");
            foreach (var element in elements)
            {
                var text = element.TextContent.Trim();
                if (!text.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;

                // Get value from next sibling or adjacent element
                var next = element.NextElementSibling;
                if (next != null)
                {
                    var val = next.TextContent.Trim();
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }

                // Or extract value after colon
                var colonIdx = text.IndexOf(':');
                if (colonIdx >= 0 && colonIdx < text.Length - 1)
                    return text[(colonIdx + 1)..].Trim();
            }
        }
        return null;
    }

    private static string? ExtractDescription(IDocument document)
    {
        // Look for Objektbeschreibung section
        var headers = document.QuerySelectorAll("h2, h3, h4, strong");
        foreach (var header in headers)
        {
            var text = header.TextContent.Trim();
            if (!text.Contains("Beschreibung", StringComparison.OrdinalIgnoreCase)
                && !text.Contains("Objektbeschreibung", StringComparison.OrdinalIgnoreCase))
                continue;

            // Get text from the next element(s)
            var parent = header.ParentElement;
            var nextSibling = header.NextElementSibling ?? parent?.NextElementSibling;
            if (nextSibling != null)
            {
                var desc = nextSibling.TextContent.Trim();
                if (desc.Length > 20)
                    return desc.Length > 10000 ? desc[..10000] : desc;
            }
        }

        // Fallback: look for large text blocks that look like descriptions
        var paragraphs = document.QuerySelectorAll("p, div")
            .Where(e => e.TextContent.Trim().Length > 100)
            .OrderByDescending(e => e.TextContent.Trim().Length)
            .Take(3);

        foreach (var p in paragraphs)
        {
            var desc = p.TextContent.Trim();
            if (desc.Length > 200 && !desc.Contains("mtmDataLayer") && !desc.Contains("function("))
            {
                return desc.Length > 10000 ? desc[..10000] : desc;
            }
        }

        return null;
    }

    private static List<string> ExtractImageUrls(IDocument document)
    {
        var imageUrls = new List<string>();

        // Extract from img tags with justimmo storage URLs
        var images = document.QuerySelectorAll("img[src]");
        foreach (var img in images)
        {
            var src = img.GetAttribute("src") ?? "";
            if (IsPropertyImage(src) && !imageUrls.Contains(src))
                imageUrls.Add(src);

            // Also check data-src for lazy-loaded images
            var dataSrc = img.GetAttribute("data-src") ?? "";
            if (IsPropertyImage(dataSrc) && !imageUrls.Contains(dataSrc))
                imageUrls.Add(dataSrc);
        }

        // Extract from source elements (picture/srcset)
        var sources = document.QuerySelectorAll("source[srcset]");
        foreach (var source in sources)
        {
            var srcset = source.GetAttribute("srcset") ?? "";
            var urls = srcset.Split(',')
                .Select(s => s.Trim().Split(' ')[0])
                .Where(IsPropertyImage);
            foreach (var u in urls)
            {
                if (!imageUrls.Contains(u))
                    imageUrls.Add(u);
            }
        }

        // Also check <a> links to images
        var links = document.QuerySelectorAll("a[href]");
        foreach (var link in links)
        {
            var href = link.GetAttribute("href") ?? "";
            if (IsPropertyImage(href) && !imageUrls.Contains(href))
                imageUrls.Add(href);
        }

        return imageUrls;
    }

    private static bool IsPropertyImage(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        var lower = url.ToLowerInvariant();
        return (lower.Contains("justimmo") || lower.Contains("storage"))
            && (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png")
                || lower.EndsWith(".webp") || lower.Contains(".jpeg?") || lower.Contains(".jpg?")
                || lower.Contains(".png?") || lower.Contains(".webp?"));
    }

    private static (string? Name, string? Phone, string? Email, string? Office) ExtractAgentInfo(IDocument document)
    {
        string? name = null;
        string? phone = null;
        string? email = null;
        string? office = null;

        // Agent name: h4 containing "Herr/Frau Firstname Lastname" (linked to /de/makler/)
        var agentLink = document.QuerySelector("a[href*='/de/makler/']");
        if (agentLink != null)
            name = agentLink.TextContent.Trim();

        // Fallback: h4 with Herr/Frau pattern
        if (string.IsNullOrEmpty(name))
        {
            foreach (var h4 in document.QuerySelectorAll("h4"))
            {
                var text = h4.TextContent.Trim();
                if (AgentNamePattern().IsMatch(text))
                {
                    name = text;
                    break;
                }
            }
        }

        // Office: link to /de/standort/ (e.g. "s REAL Rohrbach")
        var officeLink = document.QuerySelector("a[href*='/de/standort/']");
        if (officeLink != null)
            office = officeLink.TextContent.Trim();

        // Phone: link with href="tel:..."
        var phoneLink = document.QuerySelector("a[href^='tel:']");
        if (phoneLink != null)
            phone = phoneLink.TextContent.Trim();

        // Email: link with href="mailto:...@sreal.at" (exclude share/recommendation links)
        foreach (var mailLink in document.QuerySelectorAll("a[href^='mailto:']"))
        {
            var href = mailLink.GetAttribute("href") ?? "";
            var mailAddr = href["mailto:".Length..];
            // Skip share links (contain ?subject=)
            if (mailAddr.Contains("?subject=")) continue;
            if (mailAddr.Contains("@sreal.at") || mailAddr.Contains("@s-real.at"))
            {
                email = mailAddr;
                break;
            }
        }

        return (name, phone, email, office);
    }

    private static string? ExtractInfrastructure(IDocument document)
    {
        // Use the structured heading parser: "Infrastruktur" heading → list of label/value pairs
        var pairs = ParseLabelValuePairs(document, "Infrastruktur");
        return pairs.Count > 0 ? JsonSerializer.Serialize(pairs) : null;
    }

    internal static decimal? ParsePrice(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Remove currency symbols and whitespace
        var cleaned = text.Replace("€", "").Replace("EUR", "").Replace("\u00A0", "")
            .Replace(" ", "").Trim();

        // Handle "Kaufpreis" prefix
        cleaned = cleaned.Replace("Kaufpreis", "");

        // Austrian format: Punkt = Tausendertrenner, Komma = Dezimaltrenner
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        return decimal.TryParse(cleaned, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result : null;
    }

    internal static decimal? ParseArea(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var cleaned = text.Replace("m²", "").Replace("m2", "").Replace("\u00A0", "")
            .Replace(" ", "").Trim();

        // Austrian format
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        return decimal.TryParse(cleaned, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result : null;
    }

    /// <summary>
    /// Strips the " - 964/31511" object ID suffix from address text
    /// </summary>
    internal static string StripObjectIdSuffix(string text)
    {
        // Pattern: "Altenfelden - 964/31511" → "Altenfelden"
        var match = ObjectIdSuffixPattern().Match(text);
        return match.Success ? text[..match.Index].Trim() : text.Trim();
    }

    private static SrealObjectType GuessObjectTypeFromUrl(string url, string title)
    {
        var lower = (url + " " + title).ToLowerInvariant();
        if (lower.Contains("grundst") || lower.Contains("property") || lower.Contains("baugrund"))
            return SrealObjectType.Land;
        if (lower.Contains("ferien") || lower.Contains("vacation") || lower.Contains("chalet")
            || lower.Contains("almh"))
            return SrealObjectType.Vacation;
        return SrealObjectType.House;
    }

    // === Regex Patterns ===

    [GeneratedRegex(@"/de/immobilie/([^/]+)/")]
    private static partial Regex ImmobilieUrlPattern();

    [GeneratedRegex(@"/de/immobilie/([^/]+)/")]
    private static partial Regex ImmobilieIdPattern();

    [GeneratedRegex(@"^(\d{4})\s")]
    private static partial Regex PlzPattern();

    [GeneratedRegex(@"[\d.,]+\s*€")]
    private static partial Regex PricePattern();

    [GeneratedRegex(@"([\d.,]+)\s*m[²2]")]
    private static partial Regex AreaPattern();

    [GeneratedRegex(@"(\d+)")]
    private static partial Regex IntPattern();

    [GeneratedRegex(@"'(\w+)'\s*:\s*'([^']*)'")]
    private static partial Regex DataLayerPattern();

    [GeneratedRegex(@"\+43[\s()\d\-/]+\d")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"(Herr|Frau)\s+\w+\s+\w+")]
    private static partial Regex AgentNamePattern();

    [GeneratedRegex(@"(.+?)\s*[<>≤≥]\s*(\d+\s*km)")]
    private static partial Regex InfrastructurePattern();

    [GeneratedRegex(@"\s*-\s*\d{3,}/\d+")]
    private static partial Regex ObjectIdSuffixPattern();
}
