using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 5: Vorschau &amp; Veroeffentlichen. Zeigt die Zusammenfassung samt
/// Checkliste; das Veroeffentlichen laeuft serverseitig ueber PublishPropertyDraft
/// (CreateProperty + Entwurfs-Loeschung in einem Aufruf).
/// </summary>
public partial class PropertyWizardViewModel
{
    #region Vorschau (beim Betreten des Schritts aktualisiert)

    public string PreviewTitleText => string.IsNullOrWhiteSpace(Titel) ? "(Noch kein Titel)" : Titel.Trim();

    public string PreviewPriceText => decimal.TryParse(Preis, out var p) && p > 0 ? $"€ {p:N0}" : "–";

    public string PreviewLocationText
    {
        get
        {
            var parts = new[] { Adresse.Trim(), Ort.SelectedOrtText }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var text = string.Join(", ", parts);
            return string.IsNullOrEmpty(text) ? "–" : text;
        }
    }

    public string PreviewTypeText => SelectedPropertyTypeItem?.DisplayName ?? "–";

    public string PreviewDetailsText
    {
        get
        {
            var parts = new List<string>();
            if (IsHouseType && !string.IsNullOrWhiteSpace(Zimmer))
                parts.Add($"{Zimmer.Trim()} Zimmer");
            if (IsHouseType && !string.IsNullOrWhiteSpace(Wohnflaeche))
                parts.Add($"{Wohnflaeche.Trim()} m² Wohnfläche");
            if (!string.IsNullOrWhiteSpace(Grundstuecksflaeche))
                parts.Add($"{Grundstuecksflaeche.Trim()} m² Grund");
            if (IsHouseType && !string.IsNullOrWhiteSpace(Baujahr))
                parts.Add($"Bj. {Baujahr.Trim()}");
            return string.Join(" · ", parts);
        }
    }

    public bool HasPreviewDetails => !string.IsNullOrEmpty(PreviewDetailsText);

    #endregion

    #region Checkliste

    public bool ChecklistPhotosOk => Media.Any(m => m.IsPhoto);
    public bool ChecklistTitleOk => !string.IsNullOrWhiteSpace(Titel) && Titel.Trim().Length >= 10;
    public bool ChecklistDescriptionOk => ValidateDescriptionText();
    public bool ChecklistPriceOk => decimal.TryParse(Preis, out var p) && p > 0;
    public bool ChecklistLocationOk => !string.IsNullOrWhiteSpace(Adresse) && Ort.SelectedGemeindeId.HasValue;

    public bool CanPublish => ChecklistPhotosOk && ChecklistTitleOk && ChecklistDescriptionOk
        && ChecklistPriceOk && ChecklistLocationOk;

    partial void OnCurrentStepChanged(int value)
    {
        if (value == StepCount - 1)
            RefreshPreview();
    }

    private void RefreshPreview()
    {
        OnPropertyChanged(nameof(PreviewTitleText));
        OnPropertyChanged(nameof(PreviewPriceText));
        OnPropertyChanged(nameof(PreviewLocationText));
        OnPropertyChanged(nameof(PreviewTypeText));
        OnPropertyChanged(nameof(PreviewDetailsText));
        OnPropertyChanged(nameof(HasPreviewDetails));
        OnPropertyChanged(nameof(ChecklistPhotosOk));
        OnPropertyChanged(nameof(ChecklistTitleOk));
        OnPropertyChanged(nameof(ChecklistDescriptionOk));
        OnPropertyChanged(nameof(ChecklistPriceOk));
        OnPropertyChanged(nameof(ChecklistLocationOk));
        OnPropertyChanged(nameof(CanPublish));
    }

    #endregion

    #region Veroeffentlichen

    [RelayCommand]
    private async Task PublishAsync()
    {
        ErrorMessage = null;

        if (!ValidateDetails() || !ValidateLocationPrice())
            return;

        if (!ValidateDescriptionText())
        {
            ErrorMessage = IsGenerationRunning
                ? "Ihre Beschreibung wird noch erstellt – bitte einen Moment warten oder selbst schreiben."
                : "Die Beschreibung muss mindestens 50 Zeichen lang sein.";
            return;
        }

        if (!ChecklistPhotosOk)
        {
            ErrorMessage = "Bitte fügen Sie mindestens ein Foto hinzu";
            return;
        }

        if (!_authService.IsSeller)
        {
            ErrorMessage = "Inserate erstellen ist nur mit einem Verkäufer-Konto möglich.";
            return;
        }

        IsBusy = true;
        BusyMessage = "Inserat wird veröffentlicht…";
        var publishSucceeded = false;

        try
        {
            await EnsureMediaUploadedAsync();
            if (UploadedImageUrls.Count == 0)
            {
                ErrorMessage = "Die Fotos konnten nicht hochgeladen werden. Bitte versuchen Sie es erneut.";
                return;
            }

            // Finalen Stand sichern - der Server veroeffentlicht den gespeicherten Entwurf
            if (!await SaveDraftAsync() || _serverDraftId is not { } draftId)
            {
                ErrorMessage = "Der Entwurf konnte nicht gespeichert werden. Bitte versuchen Sie es erneut.";
                return;
            }

            var (_, result) = await _mediator.Request(new PublishPropertyDraftHttpRequest
            {
                Body = new PublishPropertyDraftRequest { Id = draftId }
            });

            if (result == null)
                throw new InvalidOperationException("Keine Antwort vom Server.");

            publishSucceeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler beim Veroeffentlichen");
            ErrorMessage = "Das Inserat konnte nicht veröffentlicht werden. Bitte versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }

        if (publishSucceeded)
        {
            _logger.LogInformation("[PropertyWizard] Inserat veroeffentlicht, Navigation zu MyProperties");
            StopDescriptionPolling();

            // Erst die gepushte Wizard-Seite vom Stack der Ursprungs-Section nehmen,
            // dann zur MyProperties-Root wechseln - sonst bliebe der Wizard beim
            // Rueckwechsel in die Ursprungs-Section sichtbar.
            await _navigator.PopToRoot();
            await _navigator.NavigateTo("MyProperties", relativeNavigation: false);
        }
    }

    #endregion
}
