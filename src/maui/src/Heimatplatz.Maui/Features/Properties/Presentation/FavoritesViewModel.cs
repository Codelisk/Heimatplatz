using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Services;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
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
    IPropertyStatusService propertyStatusService,
    INavigator navigator,
    IDialogs dialogs,
    ILogger<FavoritesViewModel> logger,
    CollectionStringsLocalized collectionLoc,
    CommonStringsLocalized commonLoc,
    FavoritesStringsLocalized loc,
    PropertyDetailPreloader detailPreloader
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger, collectionLoc, commonLoc, detailPreloader)
{
    public FavoritesStringsLocalized Loc => loc;

    // Merken/Entfernen auf der Detailseite zieht diese Liste nach
    protected override PropertyStatusKind? StatusKind => PropertyStatusKind.Favorite;

    // Entfernen ist mit einem Tipp umkehrbar - wie beim Aufheben einer Blockierung
    // keine Rueckfrage (Dialoge nur vor Destruktivem, z.B. Inserat loeschen)
    protected override bool ConfirmBeforeRemove => false;

    protected override string LoadingMessage => loc.LoadingMessage;
    protected override string RemovingMessage => loc.RemovingMessage;
    protected override string RemoveErrorTitle => loc.RemoveErrorTitle;

    protected override string GetRemoveErrorMessage(string errorDetails)
        => loc.RemoveErrorMessageFormat(errorDetails);

    protected override string GetLoadErrorMessage(string errorDetails)
        => loc.LoadErrorMessageFormat(errorDetails);

    protected override Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct)
        => FetchPageViaAsync(
            new GetUserFavoritesHttpRequest { Page = page, PageSize = pageSize },
            static r => (r.Properties, r.HasMore, r.Total),
            forceRemoteRefresh, ct);

    protected override async Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
    {
        var result = await RemoveViaAsync(
            new RemoveFavoriteHttpRequest { PropertyId = propertyId },
            static r => (r.Success == true, r.Message));

        if (result.Success)
            propertyStatusService.NotifyFavoriteRemoved(propertyId);

        return result;
    }
}
