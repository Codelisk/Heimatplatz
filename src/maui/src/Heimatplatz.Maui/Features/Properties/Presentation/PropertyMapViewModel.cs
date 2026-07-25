using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Vollbild-Kartenansicht der Immobiliensuche: zeigt die Web-Faltkarte
/// (/karte-embed) im WebView - gleicher Papier-Stil, gleiche Bezirks-Stempel,
/// Preis-Schilder und Mini-Zettel wie im Web, ohne die Kartenlogik zu
/// duplizieren. Die aktuellen Listen-Filter reisen als Query-Params mit.
/// Detail-Links des Mini-Zettels werden abgefangen und oeffnen die NATIVE
/// Detailseite (Property bzw. Zwangsversteigerung ueber den typ-Hint).
/// </summary>
[ShellMap<PropertyMapPage>("PropertyMap")]
public partial class PropertyMapViewModel(
    INavigator navigator,
    IFilterStateService filterState,
    IApiEndpointService apiEndpoints,
    PropertyMapStringsLocalized loc,
    ILogger<PropertyMapViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    private bool _openingListing;

    public PropertyMapStringsLocalized Loc => loc;

    [ObservableProperty]
    public partial WebViewSource? MapSource { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsOffline { get; set; }

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    public void OnAppearing()
    {
        // Rueckkehr von der Detailseite: Karte (inkl. Zoom/Mini-Zettel) bleibt stehen
        _openingListing = false;
        if (MapSource is null) LoadMap();
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    private void Retry() => LoadMap();

    private void LoadMap()
    {
        HasLoadError = false;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            // Die Karte ist das einzige reine Online-Feature der App - klarer
            // Hinweis statt WebView-Fehlerseite
            IsOffline = true;
            IsLoading = false;
            return;
        }

        IsOffline = false;
        IsLoading = true;
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var query = MapEmbedLink.BuildQuery(filterState.CurrentState, dark);
        var url = WebLinks.MapEmbedUrl(apiEndpoints.CurrentUrl, query);
        logger.LogInformation("[PropertyMap] Lade Karten-Embed: {Url}", url);
        MapSource = new UrlWebViewSource { Url = url.ToString() };
    }

    /// <summary>WebView meldet abgeschlossene Navigation (Erfolg oder Fehler).</summary>
    public void OnWebNavigated(WebNavigationResult result)
    {
        // Cancel = unsere eigenen Detail-Link-Intercepts, kein Ladefehler
        if (result == WebNavigationResult.Cancel) return;
        IsLoading = false;
        HasLoadError = result != WebNavigationResult.Success;
    }

    /// <summary>
    /// Detail-Links des Mini-Zettels abfangen (/immobilien/angebote/{id}/?typ=...):
    /// true = Navigation wurde behandelt, der WebView soll sie abbrechen.
    /// </summary>
    public bool TryHandleListingLink(string? url)
    {
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 3 || segments[0] != "immobilien" || segments[1] != "angebote") return false;
        if (!Guid.TryParse(segments[2], out var propertyId)) return false;

        if (_openingListing) return true; // Doppel-Tap: nicht zweimal navigieren
        _openingListing = true;

        // typ-Hint aus dem Embed (zv = Zwangsversteigerung); ohne Hint normale Detailseite
        var isForeclosure = uri.Query.Contains("typ=zv", StringComparison.OrdinalIgnoreCase);
        logger.LogInformation("[PropertyMap] Oeffne native Detailseite {PropertyId} (ZV: {IsForeclosure})", propertyId, isForeclosure);

        _ = isForeclosure
            ? navigator.NavigateTo<ForeclosureDetailViewModel>(vm => vm.PropertyId = propertyId.ToString("D"))
            : navigator.NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = propertyId.ToString("D"));
        return true;
    }
}
