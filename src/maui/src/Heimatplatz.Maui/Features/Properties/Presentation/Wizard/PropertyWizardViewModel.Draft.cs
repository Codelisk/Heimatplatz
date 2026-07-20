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
/// Der Zustand der Beschreibungs-Generierung liegt NICHT im Payload, sondern in eigenen
/// Entwurfs-Spalten am Server (siehe RestoreDescriptionState in .Description).
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
        // Edit-Modus arbeitet direkt am veroeffentlichten Inserat - niemals Entwuerfe anlegen
        if (IsEditMode)
            return true;

        // Store-Screenshots: die Demo-Vorbefuellung darf keine Server-Entwuerfe
        // hinterlassen (wuerde bei jedem Lauf einen weiteren Entwurf anlegen)
        if (Core.Screenshots.ScreenshotMode.IsActive)
            return true;

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
        BusyMessage = Loc.BusyLoadingDraft;

        try
        {
            var (_, response) = await _mediator.Request(new GetPropertyDraftHttpRequest { Id = draftId });
            if (response?.Data == null)
            {
                ErrorMessage = Loc.DraftLoadError;
                return;
            }

            _serverDraftId = response.Id;
            ApplyPayload(response.Data);

            // Beschreibungs-Zustand kommt aus eigenen Spalten (nicht aus dem Payload);
            // ein offener Job wird weitergepollt
            RestoreDescriptionState(response);

            // Unveraenderten Zustand nicht sofort wieder speichern
            _lastSavedPayloadJson = JsonSerializer.Serialize(BuildPayload());
            _draftDirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Entwurf konnte nicht geladen werden");
            ErrorMessage = Loc.DraftLoadError;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>Kompletter Editor-Zustand -> Entwurfs-Payload (StepIndex 0 = Ein-Seiten-Editor).</summary>
    private PropertyDraftData BuildPayload() => new()
    {
        SchemaVersion = 3,
        StepIndex = 0,
        ImageUrls = UploadedImageUrls,
        VideoUrls = UploadedVideoUrls,
        Type = SelectedPropertyTypeItem?.Value,
        Title = NullIfEmpty(Titel),
        Rooms = ParseIntOrNull(Zimmer),
        LivingAreaSquareMeters = ParseIntOrNull(Wohnflaeche),
        PlotAreaSquareMeters = ParseIntOrNull(Grundstuecksflaeche),
        YearBuilt = ParseIntOrNull(Baujahr),
        Features = FeatureItems.Count > 0 ? FeatureItems.ToList() : null,
        Address = NullIfEmpty(Adresse),
        MunicipalityId = Ort.SelectedGemeindeId,
        MunicipalityDisplay = NullIfEmpty(Ort.SelectedOrtText),
        Price = decimal.TryParse(Preis, out var preis) && preis > 0 ? (double)preis : null,
        OriginalListingUrl = NullIfEmpty(OriginalListingUrl),
        ContactName = NullIfEmpty(ContactName),
        ContactEmail = NullIfEmpty(ContactEmail),
        ContactPhone = NullIfEmpty(ContactPhone),
        DescriptionMode = DescriptionMode,
        Description = NullIfEmpty(Beschreibung),
        DescriptionKeywords = NullIfEmpty(DescriptionKeywords)
    };

    /// <summary>Entwurfs-Payload -> Editor-Zustand (Resume).</summary>
    private void ApplyPayload(PropertyDraftData data)
    {
        Media.Clear();
        foreach (var url in data.ImageUrls ?? [])
            Media.Add(WizardMediaItem.FromRemote(url, isVideo: false));
        foreach (var url in data.VideoUrls ?? [])
            Media.Add(WizardMediaItem.FromRemote(url, isVideo: true));
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));

        SelectedPropertyTypeItem = PropertyTypes.FirstOrDefault(t => t.Value == data.Type) ?? PropertyTypes[0];

        Titel = data.Title ?? string.Empty;
        Zimmer = data.Rooms?.ToString() ?? string.Empty;
        Wohnflaeche = data.LivingAreaSquareMeters?.ToString() ?? string.Empty;
        Grundstuecksflaeche = data.PlotAreaSquareMeters?.ToString() ?? string.Empty;
        Baujahr = data.YearBuilt?.ToString() ?? string.Empty;
        SetFeatures(data.Features ?? []);

        Adresse = data.Address ?? string.Empty;
        Preis = data.Price is { } price
            ? ((decimal)price).ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        OriginalListingUrl = data.OriginalListingUrl ?? string.Empty;
        ContactName = data.ContactName ?? string.Empty;
        ContactEmail = data.ContactEmail ?? string.Empty;
        ContactPhone = data.ContactPhone ?? string.Empty;
        IsContactPersonVisible = ContactName.Length > 0 || ContactEmail.Length > 0 || ContactPhone.Length > 0;
        if (data.MunicipalityId is { } municipalityId)
            Ort.Restore(municipalityId, data.MunicipalityDisplay ?? string.Empty);

        DescriptionMode = data.DescriptionMode ?? DraftDescriptionMode.None;
        Beschreibung = data.Description ?? string.Empty;
        DescriptionKeywords = data.DescriptionKeywords ?? string.Empty;

        HeroIndex = 0;
        RefreshHero();
        RefreshEditorState();
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // 0 waere serverseitig ungueltig (CreateProperty lehnt <= 0 ab) -> als "keine Angabe" behandeln
    private static int? ParseIntOrNull(string value)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
}
