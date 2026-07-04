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
[ShellMap<MyPropertiesPage>("MyProperties")]
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
    protected override string LoadErrorTitle => "Fehler beim Laden";

    // Immer neu laden um neu erstellte/bearbeitete Immobilien anzuzeigen
    protected override bool AlwaysReloadOnAppearing => true;

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie \"{property.Title}\" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Immobilie konnte nicht gelöscht werden: {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die Immobilien konnten nicht geladen werden: {errorDetails}";

    protected override async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var (_, response) = await Mediator.Request(
            new GetUserPropertiesHttpRequest
            {
                Page = page,
                PageSize = pageSize
            },
            ct
        );

        if (response?.Properties == null)
            return (Enumerable.Empty<PropertyListItemDto>(), false, 0);

        return (response.Properties, response.HasMore, response.Total);
    }

    protected override async Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
    {
        var result = await Mediator.Request(
            new DeletePropertyHttpRequest { Id = propertyId }
        );

        return (result.Result?.Success == true, result.Result?.Message);
    }

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
