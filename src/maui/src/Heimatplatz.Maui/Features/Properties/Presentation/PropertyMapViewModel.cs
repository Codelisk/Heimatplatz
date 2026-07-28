using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Vollbild-Kartenansicht der Immobiliensuche - seit dem MapLibre-Native-Umbau
/// komplett nativ (kein /karte-embed-WebView mehr): gleicher Papier-Stil wie das
/// Web (exportierte Style-JSONs + PMTiles der Web-Origin), die Pins kommen aber
/// ueber den AUTHENTIFIZIERTEN API-Client - blockierte Anbieter sind damit auch
/// auf der Karte ausgeblendet (S2 des MAUI-Funktionstests 27.07).
///
/// Der ViewModel haelt Daten und Zustaende (Pins, Stempel, Mini-Zettel-Sheet);
/// die Karten-Interaktion (Controller, Layer, Kamera) liegt im Code-Behind.
/// </summary>
[ShellMap<PropertyMapPage>("PropertyMap")]
public partial class PropertyMapViewModel(
    INavigator navigator,
    IFilterStateService filterState,
    ILocationService locationService,
    IMapStyleProvider mapStyleProvider,
    IMediator mediator,
    Features.Auth.IAuthService authService,
    PropertyMapStringsLocalized loc,
    ILogger<PropertyMapViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    private List<PropertyMapPinDto> _pins = [];
    private IReadOnlyList<MapStamp> _stamps = [];
    private PropertyMapPinDto? _selectedPin;
    private string? _lastFingerprint;
    private bool _openingListing;

    public PropertyMapStringsLocalized Loc => loc;

    // ── Karten-/Ladezustaende (Overlays wie beim frueheren WebView) ──────────
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsOffline { get; set; }

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    // ── Kartusche/Hinweise ───────────────────────────────────────────────────
    [ObservableProperty]
    public partial string? CountText { get; set; }

    [ObservableProperty]
    public partial bool HasCount { get; set; }

    [ObservableProperty]
    public partial string? NoteText { get; set; }

    [ObservableProperty]
    public partial bool HasNote { get; set; }

    [ObservableProperty]
    public partial bool ShowEmpty { get; set; }

    // ── Mini-Zettel (Sheet beim Pin-Tap) ─────────────────────────────────────
    [ObservableProperty]
    public partial bool IsPinSheetOpen { get; set; }

    [ObservableProperty]
    public partial string? SheetTitle { get; set; }

    [ObservableProperty]
    public partial string? SheetLocation { get; set; }

    [ObservableProperty]
    public partial string? SheetPrice { get; set; }

    [ObservableProperty]
    public partial string? SheetImageUrl { get; set; }

    [ObservableProperty]
    public partial bool SheetHasImage { get; set; }

    [ObservableProperty]
    public partial string? SheetCountdown { get; set; }

    [ObservableProperty]
    public partial bool SheetHasCountdown { get; set; }

    [ObservableProperty]
    public partial bool SheetShowApprox { get; set; }

    // ── Daten fuer die Karten-Layer (Code-Behind) ────────────────────────────
    public string? StampGeoJson { get; private set; }
    public string? ExactPinGeoJson { get; private set; }
    public string? ApproxPinGeoJson { get; private set; }
    public IReadOnlyList<MapStamp> Stamps => _stamps;

    /// <summary>Retry-Buttons der Overlays: das Code-Behind startet die Karte neu.</summary>
    public event EventHandler? RetryRequested;

    public void OnAppearing()
    {
        // Rueckkehr von der Detailseite: Karte (inkl. Zoom) bleibt stehen
        _openingListing = false;
    }

    public void OnDisappearing()
    {
    }

    [RelayCommand]
    private void Retry() => RetryRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Stil fuer das aktuelle Theme laden. Setzt die Offline-/Fehler-Zustaende;
    /// null = Overlay steht, Karte nicht aufbauen.
    /// </summary>
    public async Task<string?> LoadStyleAsync(CancellationToken cancellationToken)
    {
        HasLoadError = false;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            // Die Karte ist das einzige reine Online-Feature der App - klarer
            // Hinweis statt leerer Kartenflaeche
            IsOffline = true;
            return null;
        }

        IsOffline = false;
        try
        {
            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var result = await mapStyleProvider.GetStyleAsync(dark == true, cancellationToken);
            return result.StyleJson;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[PropertyMap] Kartenstil konnte nicht geladen werden");
            HasLoadError = true;
            return null;
        }
    }

    /// <summary>
    /// Pins fuer den aktuellen Filter-State laden (gleiche Filterquelle wie die
    /// Trefferliste). Unveraenderte Filter nutzen die vorhandenen Daten.
    /// </summary>
    public async Task<bool> EnsurePinsAsync(CancellationToken cancellationToken)
    {
        var state = filterState.CurrentState;
        // UserId gehoert zum Fingerprint: der Blockiert-Filter der Pins ist
        // nutzerabhaengig - nach Login/Logout muessen die Pins neu geladen werden
        var fingerprint = $"{authService.UserId?.ToString() ?? "anon"}|{BuildFingerprint(state)}";
        if (fingerprint == _lastFingerprint && StampGeoJson is not null)
            return true;

        try
        {
            var request = await PropertyFilterRequestBuilder.BuildMapPinsAsync(state, locationService, logger, cancellationToken);
            var (_, response) = await mediator.Request(request, cancellationToken);
            if (response is null)
            {
                HasLoadError = true;
                return false;
            }

            _pins = response.Pins?.ToList() ?? [];
            _lastFingerprint = fingerprint;

            var districtByMunicipality = await BuildDistrictMapAsync(cancellationToken);
            _stamps = MapPinGeoJson.GroupIntoStamps(_pins, districtByMunicipality, loc.OtherRegion);
            StampGeoJson = MapPinGeoJson.BuildStampFeatureCollection(_stamps);
            ExactPinGeoJson = MapPinGeoJson.BuildPinFeatureCollection(_pins.Where(p => !p.IsApproximate));
            ApproxPinGeoJson = MapPinGeoJson.BuildPinFeatureCollection(_pins.Where(p => p.IsApproximate));

            CountText = loc.TrefferFormat(_pins.Count.ToString());
            HasCount = _pins.Count > 0;
            ShowEmpty = _pins.Count == 0;

            var notes = new List<string>(2);
            if (response.WithoutCoordinates > 0)
                notes.Add(loc.NoteWithoutCoordsFormat(response.WithoutCoordinates.ToString()));
            if (response.Truncated)
                notes.Add(loc.NoteTruncatedFormat(_pins.Count.ToString()));
            NoteText = notes.Count > 0 ? string.Join(" ", notes) : null;
            HasNote = notes.Count > 0;

            logger.LogInformation("[PropertyMap] {PinCount} Pins geladen ({StampCount} Bezirks-Stempel)",
                _pins.Count, _stamps.Count);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[PropertyMap] Pins konnten nicht geladen werden");
            HasLoadError = true;
            return false;
        }
    }

    /// <summary>Pin-Tap auf der Karte: Mini-Zettel mit den Inserat-Daten oeffnen.</summary>
    public bool TryShowPin(string? idText)
    {
        if (!Guid.TryParse(idText, out var id))
            return false;

        var pin = _pins.FirstOrDefault(p => p.Id == id);
        if (pin is null)
            return false;

        _selectedPin = pin;
        SheetTitle = PropertyDisplay.DisplayTitle(pin.Title);
        SheetLocation = PropertyDisplay.LocationLine(pin.PostalCode, pin.City);
        SheetPrice = PropertyDisplay.Price((decimal)pin.Price);
        SheetImageUrl = pin.ImageUrl;
        SheetHasImage = !string.IsNullOrEmpty(pin.ImageUrl);

        var countdownDays = pin.Type == PropertyType.Foreclosure
            ? PropertyDisplay.AuctionCountdownDays(pin.AuctionDate)
            : null;
        SheetCountdown = countdownDays switch
        {
            null => null,
            0 => loc.AuctionToday,
            1 => loc.AuctionTomorrow,
            _ => loc.AuctionInDaysFormat(countdownDays.Value.ToString()),
        };
        SheetHasCountdown = SheetCountdown is not null;
        SheetShowApprox = pin.IsApproximate;

        IsPinSheetOpen = true;
        return true;
    }

    public MapStamp? FindStampByName(string? name) =>
        name is null ? null : _stamps.FirstOrDefault(s => s.Name == name);

    [RelayCommand]
    private void OpenSelectedPin()
    {
        if (_selectedPin is not { } pin) return;
        if (_openingListing) return; // Doppel-Tap: nicht zweimal navigieren
        _openingListing = true;
        IsPinSheetOpen = false;

        var isForeclosure = pin.Type == PropertyType.Foreclosure;
        logger.LogInformation("[PropertyMap] Oeffne native Detailseite {PropertyId} (ZV: {IsForeclosure})",
            pin.Id, isForeclosure);

        _ = isForeclosure
            ? navigator.NavigateTo<ForeclosureDetailViewModel>(vm => vm.PropertyId = pin.Id.ToString("D"))
            : navigator.NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = pin.Id.ToString("D"));
    }

    /// <summary>
    /// Filter-Fingerprint aus den Eingangswerten (nicht aus dem Request: dessen
    /// CreatedAfter haengt an UtcNow und waere nie stabil).
    /// </summary>
    private static string BuildFingerprint(FilterState state) => string.Join("|",
        state.IsHausSelected,
        state.IsGrundstueckSelected,
        state.IsZwangsversteigerungSelected,
        state.IsPrivateSelected,
        state.IsBrokerSelected,
        state.SelectedAgeFilter,
        string.Join(",", state.SelectedOrte.OrderBy(o => o, StringComparer.Ordinal)),
        string.Join(",", state.ExcludedSellerSourceIds.OrderBy(x => x)));

    private async Task<Dictionary<Guid, string>> BuildDistrictMapAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var province in await locationService.GetLocationsAsync(cancellationToken))
        {
            foreach (var district in province.Bezirke)
            {
                foreach (var municipality in district.Gemeinden)
                {
                    map[municipality.Id] = district.Name;
                }
            }
        }

        return map;
    }
}
