using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Presentation.Wizard;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die MyPropertiesPage - verwaltet die eigenen Immobilien des Benutzers
/// (bearbeiten, loeschen, neue hinzufuegen) sowie die Inserat-Entwuerfe des Wizards.
/// </summary>
[ShellMap<MyPropertiesPage>("MyProperties", registerRoute: false)]
public partial class MyPropertiesViewModel(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    IDialogs dialogs,
    ILogger<MyPropertiesViewModel> logger,
    CollectionStringsLocalized collectionLoc,
    CommonStringsLocalized commonLoc,
    MyPropertiesStringsLocalized loc
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger, collectionLoc, commonLoc)
{
    public MyPropertiesStringsLocalized Loc => loc;

    protected override string LoadingMessage => loc.LoadingMessage;
    protected override string RemovingMessage => loc.RemovingMessage;
    protected override string RemoveConfirmTitle => loc.RemoveConfirmTitle;
    protected override string RemoveErrorTitle => loc.RemoveErrorTitle;

    // Immer neu laden um neu erstellte/bearbeitete Immobilien anzuzeigen
    protected override bool AlwaysReloadOnAppearing => true;

    // Eigene Inserate gibt es nur fuer Verkaeufer-Konten (API: RequireSeller)
    protected override bool RequiresSellerRole => true;

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => loc.RemoveConfirmMessageFormat(property.Title);

    protected override string GetRemoveErrorMessage(string errorDetails)
        => loc.RemoveErrorMessageFormat(errorDetails);

    protected override string GetLoadErrorMessage(string errorDetails)
        => loc.LoadErrorMessageFormat(errorDetails);

    protected override Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct)
    {
        // Entwuerfe huckepack mit der ersten Seite laden (laeuft damit automatisch
        // bei OnAppearing und Pull-to-Refresh mit, ohne die Basisklasse anzufassen).
        // Die Basisklasse zaehlt Seiten 0-basiert (Reload laedt Seite 0).
        if (page == 0)
        {
            _ = LoadDraftsAsync();

            // Nach Veroeffentlichen/Bearbeiten im Wizard einmalig am Cache vorbei,
            // sonst fehlt das neue bzw. geaenderte Inserat bis zum naechsten Sync
            forceRemoteRefresh |= PropertyListRefreshSignal.Consume();
        }

        return FetchPageViaAsync(
            new GetUserPropertiesHttpRequest { Page = page, PageSize = pageSize },
            static r => (r.Properties, r.HasMore, r.Total),
            forceRemoteRefresh, ct);
    }

    protected override Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
        => RemoveViaAsync(
            new DeletePropertyHttpRequest { Id = propertyId },
            static r => (r.Success == true, r.Message));

    #region Entwuerfe

    public ObservableCollection<DraftListItem> Drafts { get; } = [];

    [ObservableProperty]
    public partial bool HasDrafts { get; set; }

    private async Task LoadDraftsAsync()
    {
        try
        {
            var (_, response) = await Mediator.Request(new GetPropertyDraftsHttpRequest());
            var items = response?.Drafts?.Select(d => new DraftListItem(
                d,
                DisplayTitle: string.IsNullOrWhiteSpace(d.Title)
                    ? loc.DraftFallbackTitleFormat(d.UpdatedAt.ToLocalTime())
                    : d.Title!,
                StepText: loc.DraftLastEditedFormat(d.UpdatedAt.ToLocalTime()))).ToList() ?? [];

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Drafts.Clear();
                foreach (var item in items)
                    Drafts.Add(item);
                HasDrafts = Drafts.Count > 0;
            });
        }
        catch (Exception ex)
        {
            // Entwuerfe sind sekundaer - Fehler duerfen die Immobilien-Liste nicht stoeren
            Logger.LogWarning(ex, "[MyProperties] Entwuerfe konnten nicht geladen werden");
        }
    }

    /// <summary>
    /// Setzt einen Entwurf im Wizard fort (springt zum gespeicherten Schritt)
    /// </summary>
    [RelayCommand]
    private Task ResumeDraft(DraftListItem draft)
        => Navigator.NavigateTo<PropertyWizardViewModel>(vm => vm.DraftId = draft.Id.ToString());

    /// <summary>
    /// Loescht einen Entwurf (Server entfernt auch die hochgeladenen Medien-Dateien)
    /// </summary>
    [RelayCommand]
    private async Task DeleteDraft(DraftListItem draft)
    {
        var confirmed = await Dialogs.Confirm(
            loc.DeleteDraftConfirmTitle,
            loc.DeleteDraftConfirmMessageFormat(draft.DisplayTitle),
            CommonLoc.Yes,
            CommonLoc.No);
        if (!confirmed) return;

        try
        {
            await Mediator.Request(new DeletePropertyDraftHttpRequest { Id = draft.Id });
            Drafts.Remove(draft);
            HasDrafts = Drafts.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MyProperties] Entwurf {DraftId} konnte nicht geloescht werden", draft.Id);
            await Dialogs.Alert(loc.DeleteDraftErrorTitle, loc.DeleteDraftErrorMessage, CommonLoc.Ok);
        }
    }

    #endregion

    /// <summary>
    /// Navigiert zum Inserat-Wizard
    /// </summary>
    [RelayCommand]
    private Task NavigateToAddProperty() => Navigator.NavigateTo("PropertyWizard");

    /// <summary>
    /// Bearbeitet die ausgewaehlte Immobilie im WYSIWYG-Editor (Wizard im Edit-Modus)
    /// </summary>
    [RelayCommand]
    private Task EditProperty(PropertyListItemDto property)
        => Navigator.NavigateTo<PropertyWizardViewModel>(vm => vm.EditPropertyId = property.Id.ToString());
}
