using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Presentation.Wizard;
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
    ILogger<MyPropertiesViewModel> logger
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger)
{
    protected override string LoadingMessage => "Lade Immobilien...";
    protected override string RemovingMessage => "Lösche Immobilie...";
    protected override string RemoveConfirmTitle => "Immobilie löschen?";
    protected override string RemoveErrorTitle => "Fehler beim Löschen";

    // Immer neu laden um neu erstellte/bearbeitete Immobilien anzuzeigen
    protected override bool AlwaysReloadOnAppearing => true;

    // Eigene Inserate gibt es nur fuer Verkaeufer-Konten (API: RequireSeller)
    protected override bool RequiresSellerRole => true;

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie \"{property.Title}\" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Immobilie konnte nicht gelöscht werden. {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die Immobilien konnten nicht geladen werden. {errorDetails}";

    protected override Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct)
    {
        // Entwuerfe huckepack mit der ersten Seite laden (laeuft damit automatisch
        // bei OnAppearing und Pull-to-Refresh mit, ohne die Basisklasse anzufassen).
        // Die Basisklasse zaehlt Seiten 0-basiert (Reload laedt Seite 0).
        if (page == 0)
            _ = LoadDraftsAsync();

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
            var items = response?.Drafts?.Select(d => new DraftListItem(d)).ToList() ?? [];

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
            "Entwurf löschen?",
            $"Möchten Sie \"{draft.DisplayTitle}\" wirklich löschen? Auch die hochgeladenen Fotos werden entfernt.");
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
            await Dialogs.Alert("Fehler beim Löschen", "Der Entwurf konnte nicht gelöscht werden. Bitte versuchen Sie es erneut.");
        }
    }

    #endregion

    /// <summary>
    /// Navigiert zum Inserat-Wizard
    /// </summary>
    [RelayCommand]
    private Task NavigateToAddProperty() => Navigator.NavigateTo("PropertyWizard");

    /// <summary>
    /// Navigiert zur EditPropertyPage um die ausgewaehlte Immobilie zu bearbeiten
    /// </summary>
    [RelayCommand]
    private Task EditProperty(PropertyListItemDto property)
        => Navigator.NavigateTo<EditPropertyViewModel>(vm => vm.PropertyId = property.Id.ToString());
}
