using System.Text.Json.Nodes;
using MapLibreNative.Maui.Handlers;
using MapLibreNative.Maui.Handlers.Geometry;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Karten-Orchestrierung der nativen MapLibre-Karte: Stil setzen (und auf den
/// asynchronen Style-Aufbau warten - vorher verwirft der Controller Sources und
/// Layer stillschweigend), Pin-/Stempel-Layer aufbauen, Taps aufloesen und die
/// Kamera auf Oberoesterreich begrenzen. Daten und Sheet-Zustand liegen im VM.
/// </summary>
public partial class PropertyMapPage : ContentPage
{
    // Zoom-Schwelle Stempel -> Preis-Pins (Web: PIN_MIN_ZOOM)
    private const float PinMinZoom = 9f;
    private const double MaxDistrictZoom = 12.5;

    // OOE_BOUNDS aus map-style.ts: Start-Ausschnitt. Die Kamera-Begrenzung nutzt
    // DIESELBE Box - SetCameraTargetBounds beschraenkt (anders als maxBounds im
    // Web) das Kamera-ZENTRUM, das darf Oberoesterreich nie verlassen. Was am
    // Rand dahinter laege, ist seit 28.07.2026 ohnehin vollstaendig maskiert
    // (Fokus nur auf OOE, hpmap-outside-dim deckt das Umland opak ab).
    private static readonly (double Lat, double Lon) OoeSw = (47.4611, 12.7492);
    private static readonly (double Lat, double Lon) OoeNe = (48.7726, 14.9922);
    private static readonly LatLng MaxBoundsNe = new(48.7726, 14.9922);
    private static readonly LatLng MaxBoundsSw = new(47.4611, 12.7492);

    // Papiertoene der Layer (Hex-Werte aus map-style.ts LIGHT/DARK)
    private static (string Paper, string Ink, string InkSoft) Tones(bool dark) => dark
        ? ("#221f1b", "#e6e1d8", "#a79d8f")
        : ("#f6f1e3", "#3a332d", "#6b6053");

    // Typ-Farben wie die Foto-Badges (themenunabhaengig): Haus, Grund, ZV=Markenrot
    private static object[] TypeColorExpression() =>
        ["match", new object[] { "get", "typ" }, "grund", "#33854A", "zv", "#DE2A2F", "#2F6E9E"];

    private IMapLibreMapController? _controller;
    private readonly TaskCompletionSource _mapReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _initCts;
    private string? _appliedStyleJson;
    private bool _initialCameraDone;
    private bool _initRunning;

    public PropertyMapPage()
    {
        InitializeComponent();

        // Systemschriftgroesse (Samsung "Schriftgroesse" etc.) auf die Karte
        // uebertragen: MapLibre multipliziert Style-px nur mit der Pixeldichte,
        // nicht mit der OS-Schriftskalierung - Kartentext wirkt sonst kleiner
        // als der restliche App-Text, der per FontAutoScaling mitwaechst.
        // Gedeckelt, weil der Faktor die gesamte Stilmetrik skaliert (auch
        // Linien, Icons, Pins). Muss vor der Handler-Erstellung stehen - der
        // Wert wird nur einmal beim Aufbau der Plattform-View gelesen.
        Map.UiScale = Math.Clamp(SystemFontScale(), 1.0, 1.3);

        Map.MapReadyCommand = new Command(OnMapReady);

        // Wie die HomePage-Sheets: FitContent misst den Inhalt selbst - ohne
        // Detent-Setup oeffnet das Panel nicht sichtbar (Shiny-Detent-Falle)
        PinSheet.FitContent = true;
    }

    private PropertyMapViewModel? Vm => BindingContext as PropertyMapViewModel;

    /// <summary>OS-Schriftskalierungsfaktor der Plattform (1.0 = Standard).</summary>
    private static double SystemFontScale()
    {
#if ANDROID
        return Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1.0;
#elif IOS || MACCATALYST
        // Dynamic Type: bevorzugte Body-Groesse relativ zum Standard (17pt)
        return UIKit.UIFont.PreferredBody.PointSize / 17.0;
#elif WINDOWS
        return new global::Windows.UI.ViewManagement.UISettings().TextScaleFactor;
#else
        return 1.0;
#endif
    }

    private void OnMapReady()
    {
        var controller = (Map.Handler as MapLibreMapHandler)?.Controller;
        if (controller is null)
            return;

        if (!ReferenceEquals(_controller, controller))
        {
            // Bei einem Handler-Neuaufbau nicht doppelt abonnieren
            if (_controller is not null)
            {
                _controller.OnMapClickReceived -= OnMapClick;
                _controller.OnDidFailLoadingMapReceived -= OnMapLoadFailed;
            }

            _controller = controller;
            controller.OnMapClickReceived += OnMapClick;
            controller.OnDidFailLoadingMapReceived += OnMapLoadFailed;
        }

        _mapReady.TrySetResult();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        if (Vm is not null)
            Vm.RetryRequested += OnRetryRequested;

        _ = InitializeMapAsync();
    }

    protected override void OnDisappearing()
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        if (Vm is not null)
            Vm.RetryRequested -= OnRetryRequested;

        _initCts?.Cancel();
        base.OnDisappearing();
    }

    private void OnRetryRequested(object? sender, EventArgs e) => _ = InitializeMapAsync();

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Theme-Wechsel = anderes Style-JSON; der Style-Reset entfernt auch alle
        // Laufzeit-Sources/-Layer, InitializeMapAsync baut beides neu auf
        _appliedStyleJson = null;
        _ = InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        if (Vm is not { } vm || _initRunning)
            return;

        _initRunning = true;
        _initCts?.Cancel();
        _initCts = new CancellationTokenSource();
        var cancellationToken = _initCts.Token;

        try
        {
            vm.IsLoading = true;

            var styleJson = await vm.LoadStyleAsync(cancellationToken);
            if (styleJson is null)
                return; // Offline-/Fehler-Overlay steht

            await _mapReady.Task.WaitAsync(cancellationToken);
            if (_controller is not { } controller)
                return;

            // Pins parallel zum Stil-Aufbau laden (unabhaengige Wege)
            var pinsTask = vm.EnsurePinsAsync(cancellationToken);

            if (!string.Equals(styleJson, _appliedStyleJson, StringComparison.Ordinal))
            {
                controller.SetStyleString(styleJson);
                _appliedStyleJson = styleJson;
            }

            if (!await WaitForStyleReadyAsync(controller, cancellationToken))
            {
                vm.HasLoadError = true;
                return;
            }

            controller.SetCameraTargetBounds(new LatLngBounds(MaxBoundsNe, MaxBoundsSw));
            if (!_initialCameraDone)
            {
                FitOverview(controller);
                _initialCameraDone = true;
            }

            if (await pinsTask)
                AddPinSourcesAndLayers(controller, vm);
        }
        catch (OperationCanceledException)
        {
            // Seite verlassen oder Neustart - kein Fehlerzustand
        }
        finally
        {
            _initRunning = false;
            if (Vm is not null)
                Vm.IsLoading = false;
        }
    }

    /// <summary>
    /// Nach SetStyleString ist der Stil erst nach dem nativen Load nutzbar -
    /// solange verwirft der Controller AddSource/AddLayer kommentarlos. Der
    /// Marker-Layer "hpmap-outline-line" steckt in allen vier Style-Varianten.
    /// </summary>
    private static async Task<bool> WaitForStyleReadyAsync(IMapLibreMapController controller, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (controller.GetStyleLayerIds().Contains("hpmap-outline-line"))
                return true;
            await Task.Delay(250, cancellationToken);
        }

        return false;
    }

    private void FitOverview(IMapLibreMapController controller)
    {
        var camera = controller.CameraForLatLngs([OoeSw, OoeNe], 24, 24, 24, 24);
        if (double.IsNaN(camera.Zoom))
            controller.JumpTo(48.12, 13.87, 7.3); // Fallback: OOE-Mitte
        else
            controller.JumpTo(camera.Lat, camera.Lon, camera.Zoom);
    }

    private void AddPinSourcesAndLayers(IMapLibreMapController controller, PropertyMapViewModel vm)
    {
        if (vm.StampGeoJson is null || vm.ExactPinGeoJson is null || vm.ApproxPinGeoJson is null)
            return;

        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var (paper, ink, inkSoft) = Tones(dark);

        // AddGeoJsonSource aktualisiert eine bestehende Quelle nur mit neuen Daten
        // (Optionen bleiben) - fuer Filterwechsel genau richtig
        controller.AddGeoJsonSource("hp-stamps", vm.StampGeoJson, null);
        controller.AddGeoJsonSource("hp-pins-exact", vm.ExactPinGeoJson, null);
        controller.AddGeoJsonSource("hp-pins-approx", vm.ApproxPinGeoJson, null);

        var existingLayers = controller.GetStyleLayerIds().ToHashSet(StringComparer.Ordinal);

        // ── Bezirks-Stempel (bis Zoom 9): Papier-Kreis + Anzahl + Bezirksname ──
        // Die INTERAKTIVEN Circle-Layer schalten ueber zoom-abhaengige Opacity um
        // (wie das Web per CSS) statt ueber Layer-min/maxZoom: der Query-Pfad des
        // Bindings wertet die Zoom-Grenzen falsch aus (Treffer fuer maxZoom-Layer
        // oberhalb der Grenze, keine fuer minZoom-Layer) - Taps auf unsichtbare
        // Layer verhindert die Zoom-Weiche in OnMapClick.
        if (!existingLayers.Contains("hp-stamp-circle"))
        {
            controller.AddCircleLayer("hp-stamp-circle", "hp-stamps", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = new object[] { "step", new object[] { "get", "count" }, 17, 8, 20.5, 20, 24 },
                ["circle-color"] = paper,
                ["circle-opacity"] = new object[] { "step", new object[] { "zoom" }, 0.96, PinMinZoom, 0 },
                ["circle-stroke-color"] = ink,
                ["circle-stroke-width"] = 1.5,
                ["circle-stroke-opacity"] = new object[] { "step", new object[] { "zoom" }, 0.65, PinMinZoom, 0 },
            }, enableInteraction: true);
        }

        if (!existingLayers.Contains("hp-stamp-count"))
        {
            controller.AddSymbolLayer("hp-stamp-count", "hp-stamps", null, null, new Dictionary<string, object?>
            {
                ["text-field"] = new object[] { "to-string", new object[] { "get", "count" } },
                ["text-font"] = new object[] { "Noto Sans Medium" },
                ["text-size"] = new object[] { "step", new object[] { "get", "count" }, 13, 8, 15, 20, 17 },
                ["text-color"] = ink,
                ["text-allow-overlap"] = true,
            }, maxZoom: PinMinZoom);
        }

        if (!existingLayers.Contains("hp-stamp-label"))
        {
            controller.AddSymbolLayer("hp-stamp-label", "hp-stamps", null, null, new Dictionary<string, object?>
            {
                ["text-field"] = new object[] { "get", "name" },
                ["text-font"] = new object[] { "Noto Sans Regular" },
                ["text-size"] = 10,
                ["text-transform"] = "uppercase",
                ["text-letter-spacing"] = 0.08,
                ["text-anchor"] = "top",
                ["text-offset"] = new object[] { 0, 2.0 },
                ["text-color"] = inkSoft,
                ["text-halo-color"] = paper,
                ["text-halo-width"] = 1.4,
            }, maxZoom: PinMinZoom);
        }

        // ── Einzel-Pins (ab Zoom 9) ────────────────────────────────────────────
        // Ungefaehre Lage: Typ-Punkt + schwebendes Preis-Schild (Web: .hpmap-pin)
        if (!existingLayers.Contains("hp-pin-approx-dot"))
        {
            controller.AddCircleLayer("hp-pin-approx-dot", "hp-pins-approx", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = 5.5,
                ["circle-color"] = TypeColorExpression(),
                ["circle-opacity"] = new object[] { "step", new object[] { "zoom" }, 0, PinMinZoom, 1 },
                ["circle-stroke-color"] = paper,
                ["circle-stroke-width"] = 1.4,
                ["circle-stroke-opacity"] = new object[] { "step", new object[] { "zoom" }, 0, PinMinZoom, 1 },
            }, enableInteraction: true);
        }

        if (!existingLayers.Contains("hp-pin-approx-price"))
        {
            controller.AddSymbolLayer("hp-pin-approx-price", "hp-pins-approx", null, null, new Dictionary<string, object?>
            {
                ["text-field"] = new object[] { "get", "preis" },
                ["text-font"] = new object[] { "Noto Sans Medium" },
                ["text-size"] = 12,
                ["text-anchor"] = "bottom",
                ["text-offset"] = new object[] { 0, -0.7 },
                ["text-color"] = ink,
                ["text-halo-color"] = paper,
                ["text-halo-width"] = 1.8,
            }, minZoom: PinMinZoom, enableInteraction: true);
        }

        // Punktgenaue Lage: kraeftiger Punkt mit weissem Ring (Web: Tropfen-Pin);
        // Preis und Details kommen wie im Web erst beim Antippen
        if (!existingLayers.Contains("hp-pin-exact-dot"))
        {
            controller.AddCircleLayer("hp-pin-exact-dot", "hp-pins-exact", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = 7.5,
                ["circle-color"] = TypeColorExpression(),
                ["circle-opacity"] = new object[] { "step", new object[] { "zoom" }, 0, PinMinZoom, 1 },
                ["circle-stroke-color"] = "#ffffff",
                ["circle-stroke-width"] = 2,
                ["circle-stroke-opacity"] = new object[] { "step", new object[] { "zoom" }, 0, PinMinZoom, 1 },
            }, enableInteraction: true);
        }
    }

    /// <summary>Tap-Aufloesung: Einzel-Pins vor Stempeln, sonst Sheet schliessen.</summary>
    private bool OnMapClick(LatLng latLng, double x, double y)
    {
        if (_controller is not { } controller || Vm is not { } vm)
            return false;

        // Zoom-Weiche statt Layer-Zoom-Grenzen: unterhalb der Pin-Schwelle sind
        // nur Stempel tappbar, darueber nur Pins (die Layer selbst sind wegen des
        // Query-Bugs des Bindings auf jeder Zoomstufe queryable, nur unsichtbar)
        if (controller.GetZoom() >= PinMinZoom)
        {
            var pinId =
                FirstFeatureProperty(controller, x, y, "hp-pin-exact-dot", "id") ??
                FirstFeatureProperty(controller, x, y, "hp-pin-approx-price", "id") ??
                FirstFeatureProperty(controller, x, y, "hp-pin-approx-dot", "id");
            if (pinId is not null)
            {
                RunOnMainThread(() => vm.TryShowPin(pinId));
                return true;
            }
        }
        else if (FirstFeatureProperty(controller, x, y, "hp-stamp-circle", "name") is { } stampName &&
                 vm.FindStampByName(stampName) is { } stamp)
        {
            RunOnMainThread(() => ZoomToStamp(controller, stamp));
            return true;
        }

        if (vm.IsPinSheetOpen)
        {
            RunOnMainThread(() => vm.IsPinSheetOpen = false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Erste Feature-Property im Tap-Umkreis (14px Toleranz - die Punkte sind
    /// bewusst klein, ein punktgenauer Query wuerde Touch-Taps oft verfehlen).
    /// </summary>
    private static string? FirstFeatureProperty(IMapLibreMapController controller, double x, double y, string layerId, string property)
    {
        const double tolerance = 14;
        var json = controller.QueryRenderedFeaturesInBox(x - tolerance, y - tolerance, x + tolerance, y + tolerance, layerId);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonNode.Parse(json)?["features"]?[0]?["properties"]?[property]?.GetValue<string>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ZoomToStamp(IMapLibreMapController controller, Services.MapStamp stamp)
    {
        // Web: fitBounds(padding 72, maxZoom 12.5) auf die Pins des Bezirks
        var camera = controller.CameraForLatLngs(stamp.PinPositions, 72, 72, 72, 72);
        if (double.IsNaN(camera.Zoom))
            return;

        controller.EaseTo(camera.Lat, camera.Lon, Math.Min(camera.Zoom, MaxDistrictZoom), durationMs: 600);
    }

    private void OnMapLoadFailed(string message)
    {
        RunOnMainThread(() =>
        {
            if (Vm is not { } vm)
                return;
            vm.HasLoadError = true;
            vm.IsLoading = false;
        });
    }

    // Controller-Events koennen vom Render-/Plattform-Thread kommen; alle
    // VM-/Kamera-Zugriffe deshalb auf den UI-Thread heben
    private static void RunOnMainThread(Action action)
    {
        if (MainThread.IsMainThread)
            action();
        else
            MainThread.BeginInvokeOnMainThread(action);
    }
}
