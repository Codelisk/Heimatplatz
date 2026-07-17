using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die FavoritesPage - verwaltet die favorisierten Immobilien des Benutzers.
/// </summary>
[ShellMap<FavoritesPage>("Favorites", registerRoute: false)]
public partial class FavoritesViewModel(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    IDialogs dialogs,
    ILogger<FavoritesViewModel> logger
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger)
{
    protected override string LoadingMessage => "Lade Favoriten...";
    protected override string RemovingMessage => "Entferne Favorit...";
    protected override string RemoveConfirmTitle => "Favorit entfernen?";
    protected override string RemoveErrorTitle => "Fehler beim Entfernen";

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie \"{property.Title}\" wirklich aus Ihren Favoriten entfernen?";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Immobilie konnte nicht aus den Favoriten entfernt werden. {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die Favoriten konnten nicht geladen werden. {errorDetails}";

    protected override Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct)
        => FetchPageViaAsync(
            new GetUserFavoritesHttpRequest { Page = page, PageSize = pageSize },
            static r => (r.Properties, r.HasMore, r.Total),
            forceRemoteRefresh, ct);

    protected override Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
        => RemoveViaAsync(
            new RemoveFavoriteHttpRequest { PropertyId = propertyId },
            static r => (r.Success == true, r.Message));
}
