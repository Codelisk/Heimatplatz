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

    // Typ-Farben wie die Foto-Badges (themenunabhaengig): Haus, Grund, ZV=Markenrot.
    // Der Punkt im ZV-Chip ist papierweiss, weil der Chip selbst markenrot ist (Web:
    // .hpmap-pin--zv), der Preistext dort ebenso.
    private static object[] ChipDotColorExpression() =>
        ["match", new object[] { "get", "typ" }, "grund", "#33854A", "zv", "#F6F1E3", "#2F6E9E"];

    private static object[] ChipTextColorExpression(string ink) =>
        ["match", new object[] { "get", "typ" }, "zv", "#FFFFFF", ink];

    private static object[] ChipImageExpression() =>
        ["match", new object[] { "get", "typ" }, "zv", "hp-chip-zv", "hp-chip"];

    private static object[] RealPinImageExpression() =>
        ["match", new object[] { "get", "typ" }, "grund", "hp-realpin-grund", "zv", "hp-realpin-zv", "hp-realpin-haus"];

    private IMapLibreMapController? _controller;
    private readonly TaskCompletionSource _mapReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _initCts;
    private string? _appliedStyleJson;
    private bool _initialCameraDone;
    private bool _initRunning;

    public PropertyMapPage()
    {
        InitializeComponent();

        // Karten-Metrik fuers Handy anheben: die Style-px der geteilten Karte
        // sind auf Desktop-Betrachtung ausgelegt (Ortsnamen 11-12px) - am
        // Telefon ergibt das nur ~1.7mm Texthoehe (nachgemessen am S24,
        // 450dpi). Google/Apple rendern Kartenlabels am Handy deutlich
        // groesser, deshalb Phone/Tablet-Baseline 1.3; UiScale skaliert die
        // GESAMTE Stilmetrik (Text, Pins, Linien) ueber die Pixeldichte
        // hinaus. Obendrauf die OS-Schriftskalierung (Samsung
        // "Schriftgroesse", Dynamic Type, Windows-Textskalierung), damit
        // Kartentext wie der restliche App-Text mitwaechst. Muss vor der
        // Handler-Erstellung stehen - wird nur einmal beim Aufbau der
        // Plattform-View gelesen.
        var uiScaleBaseline = DeviceInfo.Idiom == DeviceIdiom.Desktop ? 1.0 : 1.3;
        Map.UiScale = Math.Clamp(uiScaleBaseline * SystemFontScale(), 1.0, 1.7);

        Map.MapReadyCommand = new Command(OnMapReady);

        // Wie die HomePage-Sheets: FitContent misst den Inhalt selbst - ohne
        // Detent-Setup oeffnet das Panel nicht sichtbar (Shiny-Detent-Falle)
        PinSheet.FitContent = true;
    }

    private PropertyMapViewModel? Vm => BindingContext as PropertyMapViewModel;

    /// <summary>
    /// Physische Bildschirm-px pro Style-px der Karte (Geraetedichte × UiScale).
    /// Die Controller-API nimmt physische px entgegen; Paddings und Tap-Toleranzen
    /// hier sind aber in Web-/Style-px gedacht (Parity mit der Astro-Faltkarte).
    /// </summary>
    private double MapPixelScale => DeviceDisplay.MainDisplayInfo.Density * Map.UiScale;

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
        var pad = 24 * MapPixelScale;
        var camera = controller.CameraForLatLngs([OoeSw, OoeNe], pad, pad, pad, pad);
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

        // ── Einzel-Pins (ab Zoom 9) im Web-Look (Astro .hpmap-pin/.hpmap-realpin) ──
        // Sprites zuerst: nach jedem Style-Reset (Theme-Wechsel) sind die Images
        // weg; AddImage ersetzt Bestehendes still, daher bei jedem Aufbau setzen.
        RegisterPinSprites(controller, dark);

        // Ungefaehre Lage: Preis-Chip (Papier-Pille + Typ-Punkt + Preis) zentriert
        // auf der Position - wie das Web-Preis-Schild. Der unsichtbare Circle-Layer
        // darunter ist NUR Tap-Fänger: Symbol-Layer mit minZoom liefern im
        // Query-Pfad des Bindings keine Treffer (Query-Zoom-Bug, s. Kommentar bei
        // den Stempeln), Circle-Layer ohne Zoom-Grenzen schon.
        if (!existingLayers.Contains("hp-pin-approx-dot"))
        {
            controller.AddCircleLayer("hp-pin-approx-dot", "hp-pins-approx", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = 20,
                ["circle-opacity"] = 0,
            }, enableInteraction: true);
        }

        if (!existingLayers.Contains("hp-pin-approx-price"))
        {
            controller.AddSymbolLayer("hp-pin-approx-price", "hp-pins-approx", null, null, new Dictionary<string, object?>
            {
                ["icon-image"] = ChipImageExpression(),
                ["icon-text-fit"] = "both",
                ["icon-text-fit-padding"] = new object[] { 5.5, 10, 5.5, 10 },
                // Typ-Punkt als Bullet U+2022: das Basis-Noto-Sans der Glyph-PBFs
                // enthaelt KEIN U+25CF (Geometric Shapes = Noto Sans Symbols) -
                // damit rendert "●" schlicht gar nicht. font-scale pumpt das
                // Bullet auf Web-Punktgroesse auf.
                ["text-field"] = new object[]
                {
                    "format",
                    "• ", new Dictionary<string, object?>
                    {
                        ["text-color"] = ChipDotColorExpression(),
                        ["font-scale"] = 1.4,
                    },
                    new object[] { "get", "preis" }, new Dictionary<string, object?>(),
                },
                ["text-font"] = new object[] { "Noto Sans Medium" },
                ["text-size"] = 12.5,
                ["text-color"] = ChipTextColorExpression(ink),
            }, minZoom: PinMinZoom, enableInteraction: true);
        }

        // Punktgenaue Lage: echter Karten-Pin (Tropfen) in der Typ-Farbe, Spitze
        // auf der Adresse (Web: .hpmap-realpin); Preis kommt beim Antippen.
        // Gleiches Muster: unsichtbarer Tap-Fänger + Sprite-Layer darueber.
        if (!existingLayers.Contains("hp-pin-exact-dot"))
        {
            controller.AddCircleLayer("hp-pin-exact-dot", "hp-pins-exact", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = 22,
                ["circle-opacity"] = 0,
                // Der Tropfen ankert mit der Spitze unten - der Tap-Fänger muss
                // den Pin-KOERPER (oberhalb der Koordinate) abdecken
                ["circle-translate"] = new object[] { 0, -14 },
            }, enableInteraction: true);
        }

        if (!existingLayers.Contains("hp-pin-exact-pin"))
        {
            controller.AddSymbolLayer("hp-pin-exact-pin", "hp-pins-exact", null, null, new Dictionary<string, object?>
            {
                ["icon-image"] = RealPinImageExpression(),
                ["icon-anchor"] = "bottom",
                ["icon-allow-overlap"] = true,
            }, minZoom: PinMinZoom);
        }
    }

    /// <summary>
    /// Rastert die Pin-Sprites (Preis-Chip hell/dunkel/ZV, Tropfen-Pin je Typ) und
    /// registriert sie im Stil. Pure-C#-SDF-Raster statt Plattform-Canvas, damit
    /// derselbe Code auf Android/Windows/iOS laeuft und keine Abhaengigkeit dazukommt.
    /// </summary>
    private static void RegisterPinSprites(IMapLibreMapController controller, bool dark)
    {
        var (chipFill, chipFrame) = dark ? ("#2B2723", "#7A756C") : ("#FBF7EC", "#A89D8C");
        controller.AddSpriteImage("hp-chip", PinSprites.ChipWidth, PinSprites.ChipHeight,
            PinSprites.Chip(chipFill, chipFrame), PinSprites.Ratio);
        controller.AddSpriteImage("hp-chip-zv", PinSprites.ChipWidth, PinSprites.ChipHeight,
            PinSprites.Chip("#DE2A2F", "#DE2A2F"), PinSprites.Ratio);
        controller.AddSpriteImage("hp-realpin-haus", PinSprites.PinSize, PinSprites.PinSize,
            PinSprites.Teardrop("#2F6E9E"), PinSprites.Ratio);
        controller.AddSpriteImage("hp-realpin-grund", PinSprites.PinSize, PinSprites.PinSize,
            PinSprites.Teardrop("#33854A"), PinSprites.Ratio);
        controller.AddSpriteImage("hp-realpin-zv", PinSprites.PinSize, PinSprites.PinSize,
            PinSprites.Teardrop("#DE2A2F"), PinSprites.Ratio);
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
    /// Erste Feature-Property im Tap-Umkreis (14 Style-px Toleranz - die Punkte
    /// sind bewusst klein, ein punktgenauer Query wuerde Touch-Taps oft verfehlen).
    /// </summary>
    private string? FirstFeatureProperty(IMapLibreMapController controller, double x, double y, string layerId, string property)
    {
        var tolerance = 14 * MapPixelScale;
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

    private void ZoomToStamp(IMapLibreMapController controller, Services.MapStamp stamp)
    {
        // Web: fitBounds(padding 72, maxZoom 12.5) auf die Pins des Bezirks
        var pad = 72 * MapPixelScale;
        var camera = controller.CameraForLatLngs(stamp.PinPositions, pad, pad, pad, pad);
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

    /// <summary>
    /// Mini-SDF-Rasterizer fuer die Pin-Sprites (plattformneutral, keine
    /// Canvas-Abhaengigkeit). Alle Bitmaps sind 4x ueberabgetastet (Ratio) und
    /// werden von MapLibre ueber den Sprite-pixelRatio auf Style-px skaliert;
    /// Ausgabe ist premultipliziertes RGBA (mbgl-Konvention).
    /// </summary>
    private static class PinSprites
    {
        public const float Ratio = 4f;
        public const int ChipWidth = 320;   // 80 Style-px (icon-text-fit streckt auf den Preistext)
        public const int ChipHeight = 104;  // 26 Style-px wie das Web-Preis-Schild
        public const int PinSize = 152;     // 38 Style-px wie .hpmap-realpin

        /// <summary>Papier-Pille mit Rahmen (Web: .hpmap-pin, radius 999).</summary>
        public static byte[] Chip(string fillHex, string frameHex)
        {
            var fill = ParseHex(fillHex);
            var frame = ParseHex(frameHex);
            const double margin = 4, border = 5;
            const double bx = ChipWidth / 2.0 - margin, by = ChipHeight / 2.0 - margin;
            var radius = by; // volle Pille

            var pixels = new byte[ChipWidth * ChipHeight * 4];
            for (var y = 0; y < ChipHeight; y++)
            for (var x = 0; x < ChipWidth; x++)
            {
                var px = Math.Abs(x + 0.5 - ChipWidth / 2.0) - bx + radius;
                var py = Math.Abs(y + 0.5 - ChipHeight / 2.0) - by + radius;
                var sd = Math.Min(Math.Max(px, py), 0)
                         + Length(Math.Max(px, 0), Math.Max(py, 0)) - radius;

                var alpha = Coverage(sd);
                var fillMix = Coverage(sd + border); // innen Fuellung, aussen Rahmenband
                WritePixel(pixels, (y * ChipWidth + x) * 4, Mix(frame, fill, fillMix), alpha);
            }

            return pixels;
        }

        /// <summary>Tropfen-Pin mit weissem Saum und weissem Kern (Web: .hpmap-realpin-SVG).</summary>
        public static byte[] Teardrop(string fillHex)
        {
            var fill = ParseHex(fillHex);
            var white = (R: 255.0, G: 255.0, B: 255.0);
            const double cx = 76, cy = 60, radius = 48, holeRadius = 17, outline = 4;
            const double tipX = 76, tipY = 146;

            // Tangenten-Dreieck Kreis->Spitze (Tropfenform als Kreis+Dreieck-Union)
            var d = Length(tipX - cx, tipY - cy);
            var cosB = radius / d;
            var sinB = Math.Sqrt(1 - cosB * cosB);
            var (tax, tay) = (cx - radius * sinB, cy + radius * cosB);
            var (tbx, tby) = (cx + radius * sinB, cy + radius * cosB);

            var pixels = new byte[PinSize * PinSize * 4];
            for (var y = 0; y < PinSize; y++)
            for (var x = 0; x < PinSize; x++)
            {
                double px = x + 0.5, py = y + 0.5;
                var sdCircle = Length(px - cx, py - cy) - radius;
                var sd = Math.Min(sdCircle, SdTriangle(px, py, tipX, tipY, tax, tay, tbx, tby));

                var alpha = Coverage(sd - outline);          // weisser Saum aussen
                var fillMix = Coverage(sd);                  // Typ-Farbe innen
                var holeMix = Coverage(Length(px - cx, py - cy) - holeRadius);

                var color = Mix(white, fill, fillMix);
                color = Mix(color, white, holeMix);
                WritePixel(pixels, (y * PinSize + x) * 4, color, alpha);
            }

            return pixels;
        }

        private static double Coverage(double sd) => Math.Clamp(0.5 - sd / 1.5, 0, 1);

        private static double Length(double x, double y) => Math.Sqrt(x * x + y * y);

        private static (double R, double G, double B) ParseHex(string hex) =>
            (Convert.ToInt32(hex.Substring(1, 2), 16),
             Convert.ToInt32(hex.Substring(3, 2), 16),
             Convert.ToInt32(hex.Substring(5, 2), 16));

        private static (double R, double G, double B) Mix(
            (double R, double G, double B) a, (double R, double G, double B) b, double t) =>
            (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);

        private static void WritePixel(byte[] pixels, int offset, (double R, double G, double B) color, double alpha)
        {
            pixels[offset] = (byte)Math.Round(color.R * alpha);
            pixels[offset + 1] = (byte)Math.Round(color.G * alpha);
            pixels[offset + 2] = (byte)Math.Round(color.B * alpha);
            pixels[offset + 3] = (byte)Math.Round(alpha * 255);
        }

        /// <summary>Vorzeichenbehaftete Distanz zum Dreieck (Inigo Quilez).</summary>
        private static double SdTriangle(double px, double py,
            double ax, double ay, double bx, double by, double cx, double cy)
        {
            var (e0x, e0y) = (bx - ax, by - ay);
            var (e1x, e1y) = (cx - bx, cy - by);
            var (e2x, e2y) = (ax - cx, ay - cy);
            var (v0x, v0y) = (px - ax, py - ay);
            var (v1x, v1y) = (px - bx, py - by);
            var (v2x, v2y) = (px - cx, py - cy);

            var t0 = Math.Clamp((v0x * e0x + v0y * e0y) / (e0x * e0x + e0y * e0y), 0, 1);
            var t1 = Math.Clamp((v1x * e1x + v1y * e1y) / (e1x * e1x + e1y * e1y), 0, 1);
            var t2 = Math.Clamp((v2x * e2x + v2y * e2y) / (e2x * e2x + e2y * e2y), 0, 1);
            var (q0x, q0y) = (v0x - e0x * t0, v0y - e0y * t0);
            var (q1x, q1y) = (v1x - e1x * t1, v1y - e1y * t1);
            var (q2x, q2y) = (v2x - e2x * t2, v2y - e2y * t2);

            var s = Math.Sign(e0x * e2y - e0y * e2x);
            var dx = Math.Min(Math.Min(q0x * q0x + q0y * q0y, q1x * q1x + q1y * q1y), q2x * q2x + q2y * q2y);
            var dy = Math.Min(Math.Min(s * (v0x * e0y - v0y * e0x), s * (v1x * e1y - v1y * e1x)), s * (v2x * e2y - v2y * e2x));
            return -Math.Sqrt(dx) * Math.Sign(dy);
        }
    }
}
