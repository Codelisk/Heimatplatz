using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die BlockedPage - verwaltet die blockierten Immobilien des Benutzers.
/// Blockierte Immobilien werden in der Hauptliste ausgeblendet.
/// </summary>
[ShellMap<BlockedPage>("Blocked")]
public partial class BlockedViewModel(
    IAuthService authService,
    IMediator mediator,
    INavigator navigator,
    IDialogs dialogs,
    ILogger<BlockedViewModel> logger
) : PropertyCollectionViewModelBase(authService, mediator, navigator, dialogs, logger)
{
    protected override string LoadingMessage => "Lade blockierte Immobilien...";
    protected override string RemovingMessage => "Hebe Blockierung auf...";
    protected override string RemoveConfirmTitle => "Blockierung aufheben?";
    protected override string RemoveErrorTitle => "Fehler beim Aufheben";
    protected override string LoadErrorTitle => "Fehler beim Laden";

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie die Blockierung von \"{property.Title}\" wirklich aufheben? Die Immobilie wird wieder in der Hauptliste angezeigt.";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Blockierung konnte nicht aufgehoben werden: {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die blockierten Immobilien konnten nicht geladen werden: {errorDetails}";

    protected override async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var (_, response) = await Mediator.Request(
            new GetUserBlockedHttpRequest
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
            new RemoveBlockedHttpRequest { PropertyId = propertyId }
        );

        return (result.Result?.Success == true, result.Result?.Message);
    }
}
