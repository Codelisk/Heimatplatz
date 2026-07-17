using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Entwurfs-Verwaltung: Auto-Save bei jedem Schrittwechsel (Upsert via SavePropertyDraft),
/// Wiederherstellen ueber den DraftId-Navigationsparameter, Loeschen beim Verwerfen.
/// Speicherfehler blockieren die Navigation nie - Banner + Retry beim naechsten Wechsel.
/// </summary>
public partial class PropertyWizardViewModel
{
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private Guid? _serverDraftId;
    private string? _lastSavedPayloadJson;
    private bool _draftDirty;

    /// <summary>Letzter Speicherversuch fehlgeschlagen (nicht-blockierender Banner)</summary>
    [ObservableProperty]
    public partial bool DraftSaveFailed { get; set; }

    /// <summary>Erzwingt den naechsten Save auch bei unveraendertem Payload-JSON.</summary>
    private void MarkDraftDirty() => _draftDirty = true;

    /// <summary>
    /// Upsert des aktuellen Wizard-Zustands. Unveraenderter Zustand wird uebersprungen.
    /// Liefert false nur bei einem tatsaechlichen Speicherfehler.
    /// </summary>
    private async Task<bool> SaveDraftAsync()
    {
        if (!HasAnyInput())
            return true;

        await _saveSemaphore.WaitAsync();
        try
        {
            var payload = BuildPayload();
            var json = JsonSerializer.Serialize(payload);
            if (!_draftDirty && json == _lastSavedPayloadJson)
                return true;

            var (_, response) = await _mediator.Request(new SavePropertyDraftHttpRequest
            {
                Body = new SavePropertyDraftRequest { Id = _serverDraftId, Data = payload }
            });

            if (response == null)
                throw new InvalidOperationException("Keine Antwort vom Server.");

            _serverDraftId = response.Id;
            _lastSavedPayloadJson = json;
            _draftDirty = false;
            DraftSaveFailed = false;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyWizard] Entwurf konnte nicht gespeichert werden");
            DraftSaveFailed = true;
            return false;
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    /// <summary>Loescht den Server-Entwurf (Server entfernt auch die Medien-Dateien).</summary>
    private async Task DeleteDraftAsync()
    {
        if (_serverDraftId is not { } draftId)
            return;

        try
        {
            await _mediator.Request(new DeletePropertyDraftHttpRequest { Id = draftId });
            _serverDraftId = null;
        }
        catch (Exception ex)
        {
            // Nicht blockieren - der Entwurf bleibt dann in "Meine Immobilien" loeschbar
            _logger.LogWarning(ex, "[PropertyWizard] Entwurf konnte nicht geloescht werden");
        }
    }

    /// <summary>Stellt einen gespeicherten Entwurf wieder her und springt zum gespeicherten Schritt.</summary>
    private async Task LoadDraftAsync(Guid draftId)
    {
        IsBusy = true;
        BusyMessage = "Entwurf wird geladen…";

        try
        {
            var (_, response) = await _mediator.Request(new GetPropertyDraftHttpRequest { Id = draftId });
            if (response?.Data == null)
            {
                ErrorMessage = "Der Entwurf konnte nicht geladen werden.";
                return;
            }

            _serverDraftId = response.Id;
            ApplyPayload(response.Data);
            CurrentStep = Math.Clamp(response.Data.StepIndex, 0, StepCount - 1);

            // Unveraenderten Zustand nicht sofort wieder speichern
            _lastSavedPayloadJson = JsonSerializer.Serialize(BuildPayload());
            _draftDirty = false;

            // Offene Analyse weiter pollen - eine laengst fertige liefert sofort Finished
            if (response.Data.AnalysisId is { } analysisId && !AnalysisApplied && !AiSkipped)
                _runner.ResumePolling(analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Entwurf konnte nicht geladen werden");
            ErrorMessage = "Der Entwurf konnte nicht geladen werden.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>Kompletter Wizard-Zustand -> Entwurfs-Payload.</summary>
    private PropertyDraftData BuildPayload() => new()
    {
        SchemaVersion = 1,
        StepIndex = CurrentStep,
        ImageUrls = UploadedImageUrls,
        VideoUrls = UploadedVideoUrls,
        DictatedText = NullIfEmpty(DictatedText),
        AiSkipped = AiSkipped,
        AnalysisId = _runner.AnalysisId,
        AnalysisApplied = AnalysisApplied,
        Address = NullIfEmpty(Adresse),
        MunicipalityId = Ort.SelectedGemeindeId,
        MunicipalityDisplay = NullIfEmpty(Ort.SelectedOrtText),
        Price = decimal.TryParse(Preis, out var preis) && preis > 0 ? (double)preis : null,
        Type = SelectedPropertyTypeItem?.Value,
        Title = NullIfEmpty(Titel),
        Description = NullIfEmpty(Beschreibung),
        Rooms = ParseIntOrNull(Zimmer),
        LivingAreaSquareMeters = ParseIntOrNull(Wohnflaeche),
        PlotAreaSquareMeters = ParseIntOrNull(Grundstuecksflaeche),
        YearBuilt = ParseIntOrNull(Baujahr),
        Features = SplitFeatures(FeaturesText),
        AiSummary = NullIfEmpty(AiSummary)
    };

    /// <summary>Entwurfs-Payload -> Wizard-Zustand (Resume).</summary>
    private void ApplyPayload(PropertyDraftData data)
    {
        Media.Clear();
        foreach (var url in data.ImageUrls ?? [])
            Media.Add(WizardMediaItem.FromRemote(url, isVideo: false));
        foreach (var url in data.VideoUrls ?? [])
            Media.Add(WizardMediaItem.FromRemote(url, isVideo: true));
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));

        DictatedText = data.DictatedText ?? string.Empty;
        AiSkipped = data.AiSkipped;
        AnalysisApplied = data.AnalysisApplied;

        Adresse = data.Address ?? string.Empty;
        Preis = data.Price is { } price
            ? ((decimal)price).ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        if (data.MunicipalityId is { } municipalityId)
            Ort.Restore(municipalityId, data.MunicipalityDisplay ?? string.Empty);

        _suppressTypeTracking = true;
        SelectedPropertyTypeItem = PropertyTypes.FirstOrDefault(t => t.Value == data.Type) ?? PropertyTypes[0];
        _suppressTypeTracking = false;

        Titel = data.Title ?? string.Empty;
        Beschreibung = data.Description ?? string.Empty;
        Zimmer = data.Rooms?.ToString() ?? string.Empty;
        Wohnflaeche = data.LivingAreaSquareMeters?.ToString() ?? string.Empty;
        Grundstuecksflaeche = data.PlotAreaSquareMeters?.ToString() ?? string.Empty;
        Baujahr = data.YearBuilt?.ToString() ?? string.Empty;
        FeaturesText = data.Features != null ? string.Join(", ", data.Features) : string.Empty;
        AiSummary = data.AiSummary ?? string.Empty;
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseIntOrNull(string value)
        => int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;

    private static List<string>? SplitFeatures(string featuresText)
    {
        var features = featuresText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return features.Count > 0 ? features : null;
    }
}
