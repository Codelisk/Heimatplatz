using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Veroeffentlichen samt Checkliste. Die Checkliste speist den Hinweis in der
/// Aktionsleiste ("Noch offen: …"); eine laufende Beschreibungs-Generierung
/// blockiert das Veroeffentlichen NICHT - das Inserat geht mit Platzhalter live,
/// der Server liefert den fertigen Text nach.
/// </summary>
public partial class PropertyWizardViewModel
{
    #region Checkliste / Aktionsleisten-Hinweis

    public bool ChecklistPhotosOk => Media.Any(m => m.IsPhoto);
    public bool ChecklistTitleOk => !string.IsNullOrWhiteSpace(Titel) && Titel.Trim().Length >= 10;

    /// <summary>Beschreibung ok: eigener Text ODER die Generierung laeuft/ist fertig.</summary>
    public bool ChecklistDescriptionOk =>
        ValidateDescriptionText() || (IsGenerateMode && (IsGenerationRunning || IsGenerationFinished));

    public bool ChecklistPriceOk => decimal.TryParse(Preis, out var p) && p > 0;
    public bool ChecklistLocationOk => !string.IsNullOrWhiteSpace(Adresse) && Ort.SelectedGemeindeId.HasValue;

    public bool CanPublish => ChecklistPhotosOk && ChecklistTitleOk && ChecklistDescriptionOk
        && ChecklistPriceOk && ChecklistLocationOk;

    /// <summary>"Noch offen: Foto · Titel · …" fuer die Aktionsleiste (leer wenn komplett).</summary>
    public string PublishHintText
    {
        get
        {
            var missing = new List<string>();
            if (!ChecklistPhotosOk) missing.Add(Loc.MissingPhoto);
            if (!ChecklistTitleOk) missing.Add(Loc.MissingTitle);
            if (!ChecklistPriceOk) missing.Add(Loc.MissingPrice);
            if (!ChecklistLocationOk) missing.Add(Loc.MissingLocation);
            if (!ChecklistDescriptionOk) missing.Add(Loc.MissingDescription);
            return missing.Count == 0 ? string.Empty : Loc.PublishHintFormat(string.Join(" · ", missing));
        }
    }

    public bool HasPublishHint => !CanPublish;

    /// <summary>Statuszeile der Aktionsleiste, wenn alles komplett ist.</summary>
    public string PublishReadyText => IsEditMode
        ? Loc.PublishReadyEdit
        : IsGenerateMode && IsGenerationRunning
            ? Loc.PublishReadyGenerating
            : Loc.PublishReadyCreate;

    /// <summary>Text des Primaer-Buttons in der Aktionsleiste.</summary>
    public string PublishButtonText => IsEditMode ? Loc.SaveChangesButton : Loc.PublishButton;

    /// <summary>Aktualisiert Live-Anzeige (Badge, Zaehler) und Checklisten-Hinweis.</summary>
    private void RefreshEditorState()
    {
        OnPropertyChanged(nameof(ChecklistPhotosOk));
        OnPropertyChanged(nameof(ChecklistTitleOk));
        OnPropertyChanged(nameof(ChecklistDescriptionOk));
        OnPropertyChanged(nameof(ChecklistPriceOk));
        OnPropertyChanged(nameof(ChecklistLocationOk));
        OnPropertyChanged(nameof(CanPublish));
        OnPropertyChanged(nameof(PublishHintText));
        OnPropertyChanged(nameof(HasPublishHint));
        OnPropertyChanged(nameof(PublishReadyText));
    }

    #endregion

    #region Veroeffentlichen

    [RelayCommand]
    private async Task PublishAsync()
    {
        ErrorMessage = null;

        if (!ValidateDetails() || !ValidateLocationPrice() || !ValidateContactPerson())
            return;

        // Eine laufende Generierung blockiert nicht: das Inserat geht sofort online,
        // der fertige Text wird serverseitig nachgeliefert.
        var descriptionPending = IsGenerateMode && IsGenerationRunning && !ValidateDescriptionText();

        if (!ValidateDescriptionText() && !descriptionPending)
        {
            ErrorMessage = IsGenerateMode
                ? Loc.ValidationDescriptionGenerateFirst
                : Loc.ValidationDescriptionTooShort;
            return;
        }

        if (!string.IsNullOrWhiteSpace(Beschreibung) && Beschreibung.Trim().Length > 2000)
        {
            ErrorMessage = Loc.ValidationDescriptionTooLong;
            return;
        }

        if (!ChecklistPhotosOk)
        {
            ErrorMessage = Loc.ValidationPhotoMissing;
            return;
        }

        if (!_authService.IsSeller)
        {
            ErrorMessage = Loc.SellerAccountRequired;
            return;
        }

        if (IsEditMode)
        {
            await SaveChangesAsync();
            return;
        }

        IsBusy = true;
        BusyMessage = Loc.BusyPublishing;
        var publishSucceeded = false;

        try
        {
            await EnsureMediaUploadedAsync();
            if (UploadedImageUrls.Count == 0)
            {
                ErrorMessage = Loc.PhotosUploadFailed;
                return;
            }

            // Finalen Stand sichern - der Server veroeffentlicht den gespeicherten Entwurf
            MarkDraftDirty();
            if (!await SaveDraftAsync() || _serverDraftId is not { } draftId)
            {
                ErrorMessage = Loc.DraftSaveError;
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
            ErrorMessage = Loc.PublishFailed;
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
            CancelPendingAutoSave();

            // Kein Erfolgs-Alert: Die Zielseite zeigt das neue Inserat sofort -
            // dafuer laedt sie einmalig am LocalFirst-Cache vorbei
            PropertyListRefreshSignal.Request();

            // Erst die gepushte Editor-Seite vom Stack der Ursprungs-Section nehmen,
            // dann zur MyProperties-Root wechseln - sonst bliebe der Editor beim
            // Rueckwechsel in die Ursprungs-Section sichtbar.
            await _navigator.PopToRoot();
            await _navigator.NavigateTo("MyProperties", relativeNavigation: false);
        }
    }

    /// <summary>
    /// Weg aus der Sackgasse "Kaeufer-Konto": Im Profil laesst sich das Anbieten
    /// aktivieren, danach ist der Wizard nutzbar.
    /// </summary>
    [RelayCommand]
    private async Task OpenSellerProfileAsync()
    {
        await _navigator.NavigateTo("UserProfile", relativeNavigation: false);
    }

    #endregion
}
