using System.Globalization;
using System.Text.Json;
using Heimatplatz.Api.Features.Dashboards.Contracts.Models;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.Dashboards.Services.Widgets;

/// <summary>
/// Die EINE Stelle, an der DashboardPropertyQuery in die Properties-Requests
/// uebersetzt und KI-gelieferte Query-Werte normalisiert werden. Liste, Karte
/// und Kennzahlen eines Dashboards meinen dadurch garantiert dieselbe Menge
/// (dasselbe Prinzip wie PropertyQueryFilters im Properties-Feature).
/// </summary>
public static class PropertyQueryMapper
{
    /// <summary>Kanonische Typ-Werte im Definition-JSON</summary>
    public const string TypeHouse = "house";
    public const string TypeLand = "land";
    public const string TypeForeclosure = "foreclosure";

    /// <summary>Kanonische Anbieter-Werte im Definition-JSON</summary>
    public const string SellerPrivate = "private";
    public const string SellerBroker = "broker";

    public static readonly string[] AllowedSorts =
        ["newest", "oldest", "price-asc", "price-desc", "area-asc", "area-desc"];

    // Preisformat-Kanon "€ 520.000": Gruppentrennzeichen explizit setzen statt
    // CultureInfo("de-AT") - ICU liefert dort je nach Plattform NBSP statt Punkt.
    private static readonly NumberFormatInfo PriceFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 0
    };

    public static string FormatPrice(decimal value) =>
        $"€ {value.ToString("N0", PriceFormat)}";

    public static string FormatCount(int value) =>
        value.ToString("N0", PriceFormat);

    /// <summary>
    /// Normalisiert eine KI-gelieferte Query fail-closed: unbekannte Typ-/Anbieter-/
    /// Sortierwerte werden verworfen (mit Warnung), Zahlen auf sinnvolle Bereiche
    /// gekappt, Freitexte getrimmt. Null ergibt eine leere Default-Query.
    /// </summary>
    public static DashboardPropertyQuery Sanitize(DashboardPropertyQuery? query, int maxListItems, List<string> warnings)
    {
        query ??= new DashboardPropertyQuery();

        query.Types = SanitizeTypes(query.Types, warnings);
        query.Sellers = SanitizeSellers(query.Sellers, warnings);

        query.Locations = query.Locations?
            .Select(l => l?.Trim() ?? "")
            .Where(l => l.Length is > 0 and <= 100)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        if (query.Locations is { Count: 0 })
            query.Locations = null;

        // MunicipalityIds setzt ausschliesslich der Validator (Orts-Aufloesung) -
        // von der KI gelieferte IDs werden nie uebernommen.
        query.MunicipalityIds = null;

        query.PriceMin = SanitizePositive(query.PriceMin);
        query.PriceMax = SanitizePositive(query.PriceMax);
        if (query.PriceMin.HasValue && query.PriceMax.HasValue && query.PriceMin > query.PriceMax)
            (query.PriceMin, query.PriceMax) = (query.PriceMax, query.PriceMin);

        query.AreaMin = SanitizePositive(query.AreaMin);
        query.AreaMax = SanitizePositive(query.AreaMax);
        if (query.AreaMin.HasValue && query.AreaMax.HasValue && query.AreaMin > query.AreaMax)
            (query.AreaMin, query.AreaMax) = (query.AreaMax, query.AreaMin);

        query.RoomsMin = SanitizePositive(query.RoomsMin);

        query.SearchText = string.IsNullOrWhiteSpace(query.SearchText)
            ? null
            : query.SearchText.Trim() is { Length: > 200 } longText ? longText[..200] : query.SearchText.Trim();

        if (query.Sort is not null)
        {
            var sort = query.Sort.Trim().ToLowerInvariant();
            if (AllowedSorts.Contains(sort))
            {
                query.Sort = sort;
            }
            else
            {
                warnings.Add($"Unbekannte Sortierung verworfen: {query.Sort}");
                query.Sort = null;
            }
        }

        query.Limit = query.Limit.HasValue
            ? Math.Clamp(query.Limit.Value, 1, maxListItems)
            : null;

        return query;
    }

    /// <summary>
    /// Uebersetzt die (bereits bereinigte) Query in einen GetPropertiesRequest.
    /// Leere Typen = Haus + Grund (Produktregel: ZV nur auf expliziten Wunsch).
    /// createdAfter setzt nur das new-listings-Widget (Zeitfenster "Neu seit ...").
    /// </summary>
    public static GetPropertiesRequest ToGetPropertiesRequest(DashboardPropertyQuery query, int pageSize, DateTime? createdAfter = null)
    {
        var (sortBy, sortDescending) = MapSort(query.Sort);

        return new GetPropertiesRequest(
            Page: 0,
            PageSize: pageSize,
            PropertyTypesJson: SerializeEnums(MapTypes(query.Types)),
            SellerTypesJson: SerializeEnums(MapSellers(query.Sellers)),
            MunicipalityIdsJson: SerializeGuids(query.MunicipalityIds),
            CreatedAfter: createdAfter,
            PriceMin: query.PriceMin,
            PriceMax: query.PriceMax,
            AreaMin: query.AreaMin,
            AreaMax: query.AreaMax,
            RoomsMin: query.RoomsMin,
            ExcludedSellerSourceIdsJson: null,
            SearchText: query.SearchText,
            IncludeNewBuildProjects: query.IncludeNewBuild,
            SortBy: sortBy,
            SortDescending: sortDescending
        );
    }

    /// <summary>Wie ToGetPropertiesRequest, aber fuer die Kennzahlen (ohne Paging/Sortierung).</summary>
    public static GetPropertyStatsRequest ToGetPropertyStatsRequest(DashboardPropertyQuery query) =>
        new(
            PropertyTypesJson: SerializeEnums(MapTypes(query.Types)),
            SellerTypesJson: SerializeEnums(MapSellers(query.Sellers)),
            MunicipalityIdsJson: SerializeGuids(query.MunicipalityIds),
            CreatedAfter: null,
            PriceMin: query.PriceMin,
            PriceMax: query.PriceMax,
            AreaMin: query.AreaMin,
            AreaMax: query.AreaMax,
            RoomsMin: query.RoomsMin,
            ExcludedSellerSourceIdsJson: null,
            SearchText: query.SearchText,
            IncludeNewBuildProjects: query.IncludeNewBuild
        );

    /// <summary>Wie ToGetPropertiesRequest, aber fuer die Karten-Pins.</summary>
    public static GetPropertyMapPinsRequest ToGetPropertyMapPinsRequest(DashboardPropertyQuery query) =>
        new(
            PropertyTypesJson: SerializeEnums(MapTypes(query.Types)),
            SellerTypesJson: SerializeEnums(MapSellers(query.Sellers)),
            MunicipalityIdsJson: SerializeGuids(query.MunicipalityIds),
            CreatedAfter: null,
            PriceMin: query.PriceMin,
            PriceMax: query.PriceMax,
            AreaMin: query.AreaMin,
            AreaMax: query.AreaMax,
            RoomsMin: query.RoomsMin,
            ExcludedSellerSourceIdsJson: null,
            SearchText: query.SearchText,
            IncludeNewBuildProjects: query.IncludeNewBuild
        );

    private static List<string>? SanitizeTypes(List<string>? types, List<string> warnings)
    {
        if (types is null || types.Count == 0)
            return null;

        var result = new List<string>();
        foreach (var raw in types)
        {
            var normalized = raw?.Trim().ToLowerInvariant() switch
            {
                "house" or "haus" or "häuser" or "haeuser" => TypeHouse,
                "land" or "grundstueck" or "grundstück" or "grundstuecke" or "grundstücke" or "plot" => TypeLand,
                "foreclosure" or "zwangsversteigerung" or "zwangsversteigerungen" or "zv" => TypeForeclosure,
                _ => null
            };

            if (normalized is null)
                warnings.Add($"Unbekannter Immobilien-Typ verworfen: {raw}");
            else if (!result.Contains(normalized))
                result.Add(normalized);
        }

        return result.Count > 0 ? result : null;
    }

    private static List<string>? SanitizeSellers(List<string>? sellers, List<string> warnings)
    {
        if (sellers is null || sellers.Count == 0)
            return null;

        var result = new List<string>();
        foreach (var raw in sellers)
        {
            var normalized = raw?.Trim().ToLowerInvariant() switch
            {
                "private" or "privat" or "privatperson" => SellerPrivate,
                "broker" or "agent" or "makler" or "agentur" => SellerBroker,
                _ => null
            };

            if (normalized is null)
                warnings.Add($"Unbekannter Anbieter-Typ verworfen: {raw}");
            else if (!result.Contains(normalized))
                result.Add(normalized);
        }

        // Beide Anbieter-Arten = kein Filter
        return result.Count is 0 or 2 ? null : result;
    }

    private static List<PropertyType> MapTypes(List<string>? types)
    {
        // Produktregel: ohne explizite Typen Haus + Grund, ZV bleibt default-aus
        if (types is null || types.Count == 0)
            return [PropertyType.House, PropertyType.Land];

        var result = new List<PropertyType>();
        foreach (var type in types)
        {
            switch (type)
            {
                case TypeHouse: result.Add(PropertyType.House); break;
                case TypeLand: result.Add(PropertyType.Land); break;
                case TypeForeclosure: result.Add(PropertyType.Foreclosure); break;
            }
        }
        return result;
    }

    private static List<SellerType> MapSellers(List<string>? sellers)
    {
        if (sellers is null || sellers.Count == 0)
            return [];

        var result = new List<SellerType>();
        foreach (var seller in sellers)
        {
            switch (seller)
            {
                case SellerPrivate:
                    result.Add(SellerType.Private);
                    break;
                case SellerBroker:
                    // "Makler" umfasst wie im Web-Filter Makler UND Hausverwaltungen
                    result.Add(SellerType.Broker);
                    result.Add(SellerType.PropertyManager);
                    break;
            }
        }
        return result;
    }

    private static (string SortBy, bool SortDescending) MapSort(string? sort) => sort switch
    {
        "oldest" => ("CreatedAt", false),
        "price-asc" => ("Price", false),
        "price-desc" => ("Price", true),
        "area-asc" => ("PlotArea", false),
        "area-desc" => ("PlotArea", true),
        _ => ("CreatedAt", true) // newest (Default)
    };

    private static string? SerializeEnums<T>(List<T> values) where T : struct, Enum =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values.Select(v => Convert.ToInt32(v)).ToList());

    private static string? SerializeGuids(List<Guid>? values) =>
        values is null || values.Count == 0 ? null : JsonSerializer.Serialize(values);

    private static decimal? SanitizePositive(decimal? value) =>
        value is > 0 ? value : null;

    private static int? SanitizePositive(int? value) =>
        value is > 0 ? value : null;
}
