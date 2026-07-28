using System.Text.Json;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Baut die API-Abfrage fuer Immobilienlisten und Treffer-Vorschauen aus derselben
/// Filterquelle. So koennen HomePage und FilterSettingsPage keine unterschiedlichen
/// Trefferzahlen fuer denselben Filter erzeugen.
/// </summary>
internal static class PropertyFilterRequestBuilder
{
    public static async Task<GetPropertiesHttpRequest> BuildAsync(
        FilterState state,
        int page,
        int pageSize,
        ILocationService locationService,
        ILogger logger,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string>? selectedOrteOverride = null)
    {
        var (sortBy, sortDescending) = state.SelectedSort switch
        {
            SortOption.Aelteste => ("CreatedAt", false),
            SortOption.PreisAuf => ("Price", false),
            SortOption.PreisAb => ("Price", true),
            SortOption.FlaecheAb => ("PlotArea", true),
            SortOption.FlaecheAuf => ("PlotArea", false),
            SortOption.PlzAuf => ("PostalCode", false),
            SortOption.PlzAb => ("PostalCode", true),
            _ => ((string?)null, true)
        };

        var request = new GetPropertiesHttpRequest
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var propertyTypes = new List<string>();
        if (state.IsHausSelected) propertyTypes.Add("House");
        if (state.IsGrundstueckSelected) propertyTypes.Add("Land");
        if (state.IsZwangsversteigerungSelected) propertyTypes.Add("Foreclosure");
        if (propertyTypes.Count is > 0 and < 3)
            request.PropertyTypesJson = JsonSerializer.Serialize(propertyTypes);

        var sellerTypes = new List<string>();
        if (state.IsPrivateSelected) sellerTypes.Add("Private");
        if (state.IsBrokerSelected)
        {
            sellerTypes.Add("Broker");
            sellerTypes.Add("PropertyManager");
        }
        if (state.IsPrivateSelected != state.IsBrokerSelected)
            request.SellerTypesJson = JsonSerializer.Serialize(sellerTypes);

        if (state.ExcludedSellerSourceIds.Count > 0)
            request.ExcludedSellerSourceIdsJson = JsonSerializer.Serialize(state.ExcludedSellerSourceIds);

        IReadOnlyCollection<string> selectedOrte = selectedOrteOverride ?? state.SelectedOrte;
        if (selectedOrte.Count > 0)
        {
            var selectedNames = selectedOrte.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var municipalities = await locationService.GetAllMunicipalitiesAsync(cancellationToken);
            var municipalityIds = municipalities
                .Where(m => selectedNames.Contains(m.Name))
                .Select(m => m.Id)
                .ToList();

            if (municipalityIds.Count == 0)
            {
                logger.LogWarning(
                    "Ort-Filter aktiv, aber keine Gemeinde-Ids aufloesbar ({Orte})",
                    string.Join(", ", selectedOrte));
                municipalityIds = [Guid.Empty];
            }

            request.MunicipalityIdsJson = JsonSerializer.Serialize(municipalityIds);
        }

        if (state.SelectedAgeFilter != AgeFilter.Alle)
        {
            request.CreatedAfter = ResolveCreatedAfter(state.SelectedAgeFilter);
        }

        return request;
    }

    /// <summary>
    /// Gleiche Filterquelle fuer die Kartenansicht: /api/properties/map-pins nutzt
    /// serverseitig exakt dieselben PropertyQueryFilters wie die Trefferliste -
    /// hier duerfen Karte und Liste deshalb ebenfalls nicht auseinanderlaufen.
    /// </summary>
    public static async Task<GetPropertyMapPinsHttpRequest> BuildMapPinsAsync(
        FilterState state,
        ILocationService locationService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPropertyMapPinsHttpRequest();

        var propertyTypes = new List<string>();
        if (state.IsHausSelected) propertyTypes.Add("House");
        if (state.IsGrundstueckSelected) propertyTypes.Add("Land");
        if (state.IsZwangsversteigerungSelected) propertyTypes.Add("Foreclosure");
        if (propertyTypes.Count is > 0 and < 3)
            request.PropertyTypesJson = JsonSerializer.Serialize(propertyTypes);

        var sellerTypes = new List<string>();
        if (state.IsPrivateSelected) sellerTypes.Add("Private");
        if (state.IsBrokerSelected)
        {
            sellerTypes.Add("Broker");
            sellerTypes.Add("PropertyManager");
        }
        if (state.IsPrivateSelected != state.IsBrokerSelected)
            request.SellerTypesJson = JsonSerializer.Serialize(sellerTypes);

        if (state.ExcludedSellerSourceIds.Count > 0)
            request.ExcludedSellerSourceIdsJson = JsonSerializer.Serialize(state.ExcludedSellerSourceIds);

        if (state.SelectedOrte.Count > 0)
        {
            var selectedNames = state.SelectedOrte.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var municipalities = await locationService.GetAllMunicipalitiesAsync(cancellationToken);
            var municipalityIds = municipalities
                .Where(m => selectedNames.Contains(m.Name))
                .Select(m => m.Id)
                .ToList();

            if (municipalityIds.Count == 0)
            {
                logger.LogWarning(
                    "Ort-Filter aktiv, aber keine Gemeinde-Ids aufloesbar ({Orte})",
                    string.Join(", ", state.SelectedOrte));
                municipalityIds = [Guid.Empty];
            }

            request.MunicipalityIdsJson = JsonSerializer.Serialize(municipalityIds);
        }

        if (state.SelectedAgeFilter != AgeFilter.Alle)
        {
            request.CreatedAfter = ResolveCreatedAfter(state.SelectedAgeFilter);
        }

        return request;
    }

    private static DateTimeOffset ResolveCreatedAfter(AgeFilter ageFilter) => ageFilter switch
    {
        AgeFilter.EinTag => DateTimeOffset.UtcNow.AddDays(-1),
        AgeFilter.EineWoche => DateTimeOffset.UtcNow.AddDays(-7),
        AgeFilter.EinMonat => DateTimeOffset.UtcNow.AddMonths(-1),
        AgeFilter.EinJahr => DateTimeOffset.UtcNow.AddYears(-1),
        _ => DateTimeOffset.MinValue
    };
}
