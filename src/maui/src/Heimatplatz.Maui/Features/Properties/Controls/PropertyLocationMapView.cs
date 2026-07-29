using Heimatplatz.Maui.Features.Properties.Services;
using MapLibreNative.Maui;
using MapLibreNative.Maui.Handlers;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// Anzuzeigende Lage der Mini-Karte: punktgenauer Pin (roter Punkt auf der
/// Adresse) oder 300-m-Umgebungskreis (ShowExactPin = false).
/// </summary>
public sealed record LocationMapPoint(double Latitude, double Longitude, bool ShowExactPin);

/// <summary>
/// Eingebettete Lage-Karte ("Lage &amp; Umgebung" der Detailseiten und die
/// Live-Vorschau des Editors) - dieselbe native MapLibre-Karte wie die
/// Vollbild-Kartenseite, aber als kleines Kartenblatt im Zettel-Rahmen:
/// punktgenaue Lagen zeigen den roten Punkt-Pin (Web: .hpdetailmap-pin),
/// ungefaehre den 300-m-Umgebungskreis in Markenrot (Web: hp-location-fill/-line).
/// Die Karten-View entsteht erst mit der ersten Position (lazy wie der
/// IntersectionObserver im Web) - Seiten ohne Karte zahlen keinen GL-Aufbau.
/// Der Seiten-Scroll bleibt fluessig: Ein-Finger-Pan ist aus (Pendant zu
/// cooperativeGestures), gezoomt wird ueber die Navigations-Buttons bzw. Pinch.
/// </summary>
public class PropertyLocationMapView : ContentView
{
    // Kamera-Parity mit PropertyLocationMap.astro: "Ungenaue Lagen nicht zu nah heranholen"
    private const double ApproximateZoom = 13.2;
    private const double ExactZoom = 15;
    private const double CircleRadiusMeters = 300;
    private const string BrandRed = "#DE2A2F";

    public static readonly BindableProperty LocationProperty = BindableProperty.Create(
        nameof(Location), typeof(LocationMapPoint), typeof(PropertyLocationMapView),
        propertyChanged: static (bindable, _, _) => ((PropertyLocationMapView)bindable).OnLocationChanged());

    public LocationMapPoint? Location
    {
        get => (LocationMapPoint?)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    private readonly Grid _mapHost = new();
    private MapLibreMap? _map;
    private IMapLibreMapController? _controller;
    private readonly TaskCompletionSource _mapReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _initCts;
    private string? _appliedStyleJson;
    private bool _initRunning;
    private bool _initQueued;
    private bool _styleReady;
    private bool _cameraSet;

    public PropertyLocationMapView()
    {
        // Zettel-Rahmen wie die Web-Detailkarte (.hpdetailmap-sheet)
        var frame = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Padding = 0,
            Content = _mapHost,
        };
        // Stroke ist Brush-typisiert: SetAppTheme<Brush> statt SetAppThemeColor
        frame.SetAppTheme<Brush>(Border.StrokeProperty,
            new SolidColorBrush((Color)Application.Current!.Resources["ZettelFrame"]),
            new SolidColorBrush((Color)Application.Current.Resources["ZettelFrameDark"]));
        frame.SetAppThemeColor(BackgroundColorProperty,
            (Color)Application.Current.Resources["SurfaceAlt"],
            (Color)Application.Current.Resources["SurfaceAltDark"]);
        Content = frame;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;

        // Rueckkehr zur Seite: Stil-/Layer-Zustand pruefen (billig, wenn intakt)
        if (_map is not null)
            _ = InitializeAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;

        _initCts?.Cancel();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (_map is null)
            return;

        // Theme-Wechsel = anderes Style-JSON; der Style-Reset entfernt auch die
        // Lage-Layer, InitializeAsync baut beides neu auf (Muster PropertyMapPage)
        _appliedStyleJson = null;
        _styleReady = false;
        _ = InitializeAsync();
    }

    private void OnLocationChanged()
    {
        if (Location is not null)
            EnsureMapCreated();

        // Stil steht schon: Position sofort uebernehmen (auch das Leeren);
        // sonst uebernimmt der laufende Init sie am Ende selbst
        if (_styleReady && _controller is { } controller)
            ApplyLocation(controller);
    }

    private void EnsureMapCreated()
    {
        if (_map is not null)
            return;

        _map = new MapLibreMap
        {
            MinZoom = 8f,
            MaxZoom = 16.5f,
            ScrollGesturesEnabled = false,
            RotateGesturesEnabled = false,
            TiltGesturesEnabled = false,
            ShowGpsControl = false,
            NavigationControlPosition = MapControlCorner.BottomLeft,
            AttributionControlPosition = MapControlCorner.BottomRight,
            // OS-Schriftskalierung wie die Vollbild-Karte; MUSS vor der
            // Handler-Erstellung stehen (wird nur einmal gelesen)
            UiScale = Math.Clamp(SystemFontScale(), 1.0, 1.5),
        };
        _map.MapReadyCommand = new Command(OnMapReady);
        _mapHost.Children.Add(_map);

        // Stil laedt parallel zum Aufbau der Plattform-View (InitializeAsync
        // wartet intern auf MapReady)
        _ = InitializeAsync();
    }

    private void OnMapReady()
    {
        var controller = (_map?.Handler as MapLibreMapHandler)?.Controller;
        if (controller is null)
            return;

        if (!ReferenceEquals(_controller, controller))
        {
            // Handler-Neuaufbau: der alte Stil-/Layer-Zustand ist weg
            _controller = controller;
            _appliedStyleJson = null;
            _styleReady = false;
            _cameraSet = false;
        }

        _mapReady.TrySetResult();

        if (!_initRunning)
            _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_initRunning)
        {
            _initQueued = true;
            return;
        }

        _initRunning = true;
        _initCts?.Cancel();
        _initCts = new CancellationTokenSource();
        var cancellationToken = _initCts.Token;

        try
        {
            if (ResolveService<IMapStyleProvider>() is not { } styleProvider)
                return;

            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var style = await styleProvider.GetStyleAsync(dark == true, cancellationToken);

            await _mapReady.Task.WaitAsync(cancellationToken);
            if (_controller is not { } controller)
                return;

            if (!string.Equals(style.StyleJson, _appliedStyleJson, StringComparison.Ordinal))
            {
                _styleReady = false;
                controller.SetStyleString(style.StyleJson);
                _appliedStyleJson = style.StyleJson;
            }

            if (!await WaitForStyleReadyAsync(controller, cancellationToken))
                return;

            _styleReady = true;
            ApplyLocation(controller);
        }
        catch (OperationCanceledException)
        {
            // Seite verlassen oder Neustart - kein Fehlerzustand
        }
        catch (Exception ex)
        {
            // Die Lage-Karte ist Komfort: still scheitern (Rahmen bleibt leer),
            // die Seite selbst funktioniert ohne sie vollstaendig
            ResolveService<ILogger<PropertyLocationMapView>>()
                ?.LogWarning(ex, "[LocationMap] Mini-Karte konnte nicht aufgebaut werden");
        }
        finally
        {
            _initRunning = false;
            if (_initQueued)
            {
                _initQueued = false;
                _ = InitializeAsync();
            }
        }
    }

    /// <summary>
    /// Nach SetStyleString ist der Stil erst nach dem nativen Load nutzbar -
    /// solange verwirft der Controller AddSource/AddLayer kommentarlos. Der
    /// Marker-Layer "hpmap-outline-line" steckt in allen vier Style-Varianten
    /// (gleiches Muster wie PropertyMapPage).
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

    /// <summary>
    /// Quellen-Daten steuern die Sichtbarkeit: die Layer stehen fest, Kreis- und
    /// Punkt-Quelle bekommen je Modus die Geometrie oder eine leere Collection.
    /// </summary>
    private void ApplyLocation(IMapLibreMapController controller)
    {
        var location = Location;

        var circleJson = location is { ShowExactPin: false }
            ? MapPinGeoJson.BuildLocationCircle(location.Latitude, location.Longitude, CircleRadiusMeters)
            : MapPinGeoJson.EmptyFeatureCollection;
        var pointJson = location is { ShowExactPin: true }
            ? MapPinGeoJson.BuildSinglePoint(location.Latitude, location.Longitude)
            : MapPinGeoJson.EmptyFeatureCollection;

        // AddGeoJsonSource aktualisiert eine bestehende Quelle nur mit neuen Daten
        controller.AddGeoJsonSource("hp-location-circle", circleJson, null);
        controller.AddGeoJsonSource("hp-location-point", pointJson, null);

        var existingLayers = controller.GetStyleLayerIds().ToHashSet(StringComparer.Ordinal);

        // Umgebungskreis wie die Web-Detailkarte (fill-opacity 0.14, line 2/0.5)
        if (!existingLayers.Contains("hp-location-fill"))
        {
            controller.AddFillLayer("hp-location-fill", "hp-location-circle", null, null, new Dictionary<string, object?>
            {
                ["fill-color"] = BrandRed,
                ["fill-opacity"] = 0.14,
            });
        }

        if (!existingLayers.Contains("hp-location-line"))
        {
            controller.AddLineLayer("hp-location-line", "hp-location-circle", null, null, new Dictionary<string, object?>
            {
                ["line-color"] = BrandRed,
                ["line-width"] = 2,
                ["line-opacity"] = 0.5,
            });
        }

        // Punkt-Pin wie .hpdetailmap-pin (22 px roter Punkt mit weissem Saum)
        if (!existingLayers.Contains("hp-location-pin"))
        {
            controller.AddCircleLayer("hp-location-pin", "hp-location-point", null, null, new Dictionary<string, object?>
            {
                ["circle-radius"] = 11,
                ["circle-color"] = BrandRed,
                ["circle-stroke-color"] = "#FFFFFF",
                ["circle-stroke-width"] = 3,
            });
        }

        if (location is null)
            return;

        var zoom = location.ShowExactPin ? ExactZoom : ApproximateZoom;
        if (_cameraSet)
        {
            controller.EaseTo(location.Latitude, location.Longitude, zoom, durationMs: 500);
        }
        else
        {
            controller.JumpTo(location.Latitude, location.Longitude, zoom);
            _cameraSet = true;
        }
    }

    private static T? ResolveService<T>() where T : class =>
        IPlatformApplication.Current?.Services.GetService<T>();

    /// <summary>OS-Schriftskalierungsfaktor (identisch zu PropertyMapPage.SystemFontScale).</summary>
    private static double SystemFontScale()
    {
#if ANDROID
        return Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1.0;
#elif IOS || MACCATALYST
        var body = UIKit.UIFont.PreferredBody;
        return body is null ? 1.0 : body.PointSize / 17.0;
#elif WINDOWS
        return new global::Windows.UI.ViewManagement.UISettings().TextScaleFactor;
#else
        return 1.0;
#endif
    }
}
