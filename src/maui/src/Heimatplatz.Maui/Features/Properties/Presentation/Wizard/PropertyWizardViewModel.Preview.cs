using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
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
            if (!ChecklistPhotosOk) missing.Add("Foto");
            if (!ChecklistTitleOk) missing.Add("Titel");
            if (!ChecklistPriceOk) missing.Add("Preis");
            if (!ChecklistLocationOk) missing.Add("Adresse & Ort");
            if (!ChecklistDescriptionOk) missing.Add("Beschreibung");
            return missing.Count == 0 ? string.Empty : $"Noch offen: {string.Join(" · ", missing)}";
        }
    }

    public bool HasPublishHint => !CanPublish;

    /// <summary>Statuszeile der Aktionsleiste, wenn alles komplett ist.</summary>
    public string PublishReadyText => IsGenerateMode && IsGenerationRunning
        ? "Bereit – die Beschreibung wird im Hintergrund fertig erstellt"
        : "Alles komplett – Ihr Inserat kann online gehen";

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

        if (!ValidateDetails() || !ValidateLocationPrice())
            return;

        // Eine laufende Generierung blockiert nicht: das Inserat geht sofort online,
        // der fertige Text wird serverseitig nachgeliefert.
        var descriptionPending = IsGenerateMode && IsGenerationRunning && !ValidateDescriptionText();

        if (!ValidateDescriptionText() && !descriptionPending)
        {
            ErrorMessage = IsGenerateMode
                ? "Bitte lassen Sie zuerst den Text erstellen – oder schreiben Sie die Beschreibung selbst."
                : "Die Beschreibung muss mindestens 50 Zeichen lang sein.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Beschreibung) && Beschreibung.Trim().Length > 2000)
        {
            ErrorMessage = "Die Beschreibung darf höchstens 2000 Zeichen lang sein.";
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
            MarkDraftDirty();
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
            CancelPendingAutoSave();

            if (descriptionPending)
            {
                // Kein Warten auf den Job: kurz erklaeren, dass der Text spaeter erscheint
                await Shell.Current.DisplayAlertAsync(
                    "Inserat veröffentlicht",
                    "Ihre Beschreibung wird gerade erstellt und erscheint in Kürze automatisch im Inserat.",
                    "OK");
            }

            // Erst die gepushte Editor-Seite vom Stack der Ursprungs-Section nehmen,
            // dann zur MyProperties-Root wechseln - sonst bliebe der Editor beim
            // Rueckwechsel in die Ursprungs-Section sichtbar.
            await _navigator.PopToRoot();
            await _navigator.NavigateTo("MyProperties", relativeNavigation: false);
        }
    }

    #endregion
}
