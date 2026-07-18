using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Localization;
using Heimatplatz.Maui.Localization.Properties;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die BlockedPage - verwaltet die blockierten Immobilien des Benutzers.
/// Blockierte Immobilien werden in der Hauptliste ausgeblendet.
/// </summary>
[ShellMap<BlockedPage>("Blocked", registerRoute: false)]
public partial class BlockedViewModel(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    IDialogs dialogs,
    ILogger<BlockedViewModel> logger,
    CollectionStringsLocalized collectionLoc,
    CommonStringsLocalized commonLoc,
    BlockedStringsLocalized loc
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger, collectionLoc, commonLoc)
{
    public BlockedStringsLocalized Loc => loc;

    protected override string LoadingMessage => loc.LoadingMessage;
    protected override string RemovingMessage => loc.RemovingMessage;
    protected override string RemoveErrorTitle => loc.RemoveErrorTitle;

    // Aufheben ist trivial umkehrbar (wieder blockieren) - keine Rueckfrage
    protected override bool ConfirmBeforeRemove => false;

    protected override string GetRemoveErrorMessage(string errorDetails)
        => loc.RemoveErrorMessageFormat(errorDetails);

    protected override string GetLoadErrorMessage(string errorDetails)
        => loc.LoadErrorMessageFormat(errorDetails);

    protected override Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct)
        => FetchPageViaAsync(
            new GetUserBlockedHttpRequest { Page = page, PageSize = pageSize },
            static r => (r.Properties, r.HasMore, r.Total),
            forceRemoteRefresh, ct);

    protected override Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
        => RemoveViaAsync(
            new RemoveBlockedHttpRequest { PropertyId = propertyId },
            static r => (r.Success == true, r.Message));
}
