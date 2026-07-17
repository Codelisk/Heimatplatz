using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die MyPropertiesPage - verwaltet die eigenen Immobilien des Benutzers
/// (bearbeiten, loeschen, neue hinzufuegen).
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
        => FetchPageViaAsync(
            new GetUserPropertiesHttpRequest { Page = page, PageSize = pageSize },
            static r => (r.Properties, r.HasMore, r.Total),
            forceRemoteRefresh, ct);

    protected override Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
        => RemoveViaAsync(
            new DeletePropertyHttpRequest { Id = propertyId },
            static r => (r.Success == true, r.Message));

    /// <summary>
    /// Navigiert zur Inseratserstellung (KI-Flow auf Phones, manuell sonst)
    /// </summary>
    [RelayCommand]
    private Task NavigateToAddProperty() => Navigator.NavigateTo(PropertyCreationRoutes.Default);

    /// <summary>
    /// Navigiert zur EditPropertyPage um die ausgewaehlte Immobilie zu bearbeiten
    /// </summary>
    [RelayCommand]
    private Task EditProperty(PropertyListItemDto property)
        => Navigator.NavigateTo<EditPropertyViewModel>(vm => vm.PropertyId = property.Id.ToString());
}
