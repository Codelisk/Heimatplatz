using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Controls;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Live-Kartenvorschau der Lage-Anzeige (Pendant zu PropertyLocationPreview.astro):
/// Adresse/Ort werden debounced ueber POST /api/properties/geocode-preview
/// aufgeloest (RequireSeller; Fehler bleiben still - die Vorschau ist Komfort).
/// Die Karte zeigt je Modus den 300-m-Umgebungskreis, den punktgenauen Pin oder
/// das Verborgen-Overlay; konnte die Adresse nur ungefaehr gefunden werden,
/// wechselt die Fussnote auf den Umgebungskreis-Hinweis (wie im Web).
/// </summary>
public partial class PropertyWizardViewModel
{
    /// <summary>Debounce wie im Web-Editor (1200 ms) - Nominatim ist auf 1 Request/s gedrosselt.</summary>
    private static readonly TimeSpan GeocodePreviewDebounce = TimeSpan.FromMilliseconds(1200);

    private CancellationTokenSource? _geocodePreviewCts;
    private string? _lastGeocodeKey;
    private (double Lat, double Lon, bool IsExact)? _previewCoords;
    private bool _geocodePreviewFailed;

    /// <summary>Position der Vorschau-Karte (null = keine aufloesbare Adresse).</summary>
    [ObservableProperty]
    public partial LocationMapPoint? PreviewLocation { get; set; }

    /// <summary>Leerzustand-Overlay: noch kein Ort gewaehlt bzw. Adresse nicht aufloesbar.</summary>
    [ObservableProperty]
    public partial bool IsPreviewEmpty { get; set; }

    /// <summary>Text im Leerzustand-Overlay (Standard bzw. Geocode-Fehlerhinweis).</summary>
    [ObservableProperty]
    public partial string? PreviewEmptyText { get; set; }

    /// <summary>Verborgen-Overlay (Modus "Nicht anzeigen") - die Karte darunter bleibt unangetastet.</summary>
    [ObservableProperty]
    public partial bool IsPreviewHidden { get; set; }

    /// <summary>Fussnote unter der Vorschau ("So sehen Besucher ..." bzw. Exakt-Fallback).</summary>
    [ObservableProperty]
    public partial string? PreviewNoteText { get; set; }

    private void InitializeLocationPreview()
    {
        IsPreviewEmpty = true;
        PreviewEmptyText = Loc.LocationPreviewEmpty;
        PreviewNoteText = Loc.LocationPreviewNote;
    }

    /// <summary>Netzrueckkehr: eine fehlgeschlagene Geocode-Vorschau automatisch neu versuchen.</summary>
    private void SubscribeGeocodeRetry() =>
        Connectivity.Current.ConnectivityChanged += OnConnectivityChangedForGeocode;

    private void UnsubscribeGeocodeRetry() =>
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChangedForGeocode;

    private void OnConnectivityChangedForGeocode(object? sender, ConnectivityChangedEventArgs e)
    {
        if (!_geocodePreviewFailed || e.NetworkAccess != NetworkAccess.Internet)
            return;

        // Event kann von einem Hintergrund-Thread kommen; der Debounce-Pfad
        // endet in ObservableProperty-Setzern (UI-Bindings)
        MainThread.BeginInvokeOnMainThread(ScheduleGeocodePreview);
    }

    /// <summary>Startet den Geocode-Debounce neu (Adress- oder Ort-Aenderung).</summary>
    private void ScheduleGeocodePreview()
    {
        _geocodePreviewCts?.Cancel();
        _geocodePreviewCts?.Dispose();
        var cts = new CancellationTokenSource();
        _geocodePreviewCts = cts;
        _ = GeocodePreviewAfterDelayAsync(cts.Token);
    }

    private async Task GeocodePreviewAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(GeocodePreviewDebounce, cancellationToken);
            await GeocodePreviewAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Neuere Eingabe hat den Timer neu gestartet
        }
    }

    private async Task GeocodePreviewAsync(CancellationToken cancellationToken)
    {
        var resolved = ResolvePreviewCity();
        if (resolved is null)
        {
            _geocodePreviewFailed = false;
            _previewCoords = null;
            _lastGeocodeKey = null;
            RenderLocationPreview();
            return;
        }

        var (city, postalCode) = resolved.Value;
        var address = string.IsNullOrWhiteSpace(Adresse) ? null : Adresse.Trim();

        // Dedupe wie im Web: unveraenderte Eingaben nur neu rendern, nicht neu geocodieren
        var key = $"{address}|{postalCode}|{city}";
        if (key == _lastGeocodeKey)
        {
            RenderLocationPreview();
            return;
        }

        try
        {
            var (_, response) = await _mediator.Request(new GeocodePropertyPreviewHttpRequest
            {
                Body = new GeocodePropertyPreviewRequest
                {
                    City = city,
                    PostalCode = postalCode,
                    Address = address,
                }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return; // veraltete Antwort - eine neuere Eingabe laeuft bereits

            _geocodePreviewFailed = false;
            _lastGeocodeKey = key;
            _previewCoords = response is { Latitude: not null, Longitude: not null }
                ? (response.Latitude.Value, response.Longitude.Value, response.IsExact)
                : null;
            RenderLocationPreview();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                return; // veraltete Anfrage - eine neuere Eingabe laeuft bereits

            _logger.LogDebug(ex, "[PropertyWizard] Geocode-Vorschau fehlgeschlagen - Fehlerzustand anzeigen, Retry bei Netzrueckkehr");

            // Der alte Pin gehoert zur vorherigen Eingabe - stehen lassen waere
            // irrefuehrend. Key zuruecksetzen, damit der Retry (Netzrueckkehr
            // oder naechste Eingabe) dieselbe Adresse erneut geocodiert.
            _geocodePreviewFailed = true;
            _previewCoords = null;
            _lastGeocodeKey = null;
            RenderLocationPreview();
        }
    }

    /// <summary>Setzt Karte, Overlays und Fussnote je Anzeige-Modus (Web: render()).</summary>
    private void RenderLocationPreview()
    {
        if (SelectedLocationDisplay == LocationDisplayMode.Hidden)
        {
            // Karte bewusst unangetastet lassen - Zurueckschalten zeigt sofort den alten Stand
            IsPreviewHidden = true;
            IsPreviewEmpty = false;
            PreviewNoteText = Loc.LocationPreviewNote;
            return;
        }

        IsPreviewHidden = false;

        if (_previewCoords is not { } coords)
        {
            IsPreviewEmpty = true;
            PreviewLocation = null;
            PreviewEmptyText = _geocodePreviewFailed ? Loc.LocationPreviewError : Loc.LocationPreviewEmpty;
            PreviewNoteText = Loc.LocationPreviewNote;
            return;
        }

        IsPreviewEmpty = false;

        // Punktgenau nur wenn gewollt UND aufloesbar - sonst Umgebungskreis mit Hinweis
        var showExactPin = SelectedLocationDisplay == LocationDisplayMode.Exact && coords.IsExact;
        PreviewLocation = new LocationMapPoint(coords.Lat, coords.Lon, showExactPin);
        PreviewNoteText = SelectedLocationDisplay == LocationDisplayMode.Exact && !coords.IsExact
            ? Loc.LocationPreviewExactFallback
            : Loc.LocationPreviewNote;
    }

    /// <summary>
    /// Ort/PLZ der gewaehlten Gemeinde fuer den Geocoder. Faellt auf das Parsen
    /// des Anzeige-Texts "Name (PLZ)" zurueck, wenn die Gemeindeliste noch nicht
    /// geladen ist (Entwurfs-Restore vor EnsureLoadedAsync).
    /// </summary>
    private (string City, string? PostalCode)? ResolvePreviewCity()
    {
        if (Ort.FindSelectedGemeinde() is { } gemeinde)
            return (gemeinde.Name, gemeinde.PostalCode);

        var text = Ort.SelectedOrtText;
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Regex.Match(text, @"^(.+)\s\((\d{4})\)$");
        return match.Success
            ? (match.Groups[1].Value, match.Groups[2].Value)
            : (text.Trim(), null);
    }
}
