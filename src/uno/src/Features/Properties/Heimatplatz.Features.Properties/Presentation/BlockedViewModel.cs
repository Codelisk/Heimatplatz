using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for BlockedPage - manages user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// Extends PropertyCollectionViewModelBase for shared collection functionality.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class BlockedViewModel : PropertyCollectionViewModelBase
{
    public BlockedViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<BlockedViewModel> logger)
        : base(authService, mediator, navigator, logger)
    {
    }

    #region Abstract Property Implementations

    public override string PageTitle => "Blockierte Immobilien";
    public override string EmptyStateIcon => "\uE8F8"; // Block icon
    public override string EmptyStateTitle => "Keine blockierten Immobilien";
    public override string EmptyStateDescription => "Immobilien, die Sie blockieren, werden hier angezeigt und aus der Hauptliste ausgeblendet.";
    protected override string LoadingMessage => "Lade blockierte Immobilien...";
    protected override string RemovingMessage => "Hebe Blockierung auf...";
    protected override string RemoveConfirmTitle => "Blockierung aufheben?";
    protected override string RemoveSuccessTitle => "Blockierung aufgehoben";
    protected override string RemoveErrorTitle => "Fehler beim Aufheben";
    protected override string LoadErrorTitle => "Fehler beim Laden";

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie die Blockierung von \"{property.Title}\" wirklich aufheben? Die Immobilie wird wieder in der Hauptliste angezeigt.";

    protected override string GetRemoveSuccessMessage(string? apiMessage)
        => apiMessage ?? "Die Blockierung wurde aufgehoben. Die Immobilie wird wieder in der Hauptliste angezeigt.";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Blockierung konnte nicht aufgehoben werden: {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die blockierten Immobilien konnten nicht geladen werden: {errorDetails}";

    #endregion

    #region Abstract Method Implementations

    protected override async Task<List<PropertyListItemDto>?> FetchPropertiesAsync()
    {
        var (context, response) = await Mediator.Request(
            new Heimatplatz.Core.ApiClient.Generated.GetUserBlockedHttpRequest()
        );

        if (response?.Properties == null)
            return null;

        return response.Properties.Select(prop => new PropertyListItemDto(
            Id: prop.Id,
            Title: prop.Title,
            Address: prop.Address,
            City: prop.City,
            Price: (decimal)prop.Price,
            LivingAreaM2: prop.LivingAreaM2,
            PlotAreaM2: prop.PlotAreaM2,
            Rooms: prop.Rooms,
            Type: (PropertyType)prop.Type,
            SellerType: (SellerType)prop.SellerType,
            SellerName: prop.SellerName,
            ImageUrls: prop.ImageUrls,
            CreatedAt: prop.CreatedAt.DateTime
        )).ToList();
    }

    protected override async Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
    {
        var result = await Mediator.Request(
            new Heimatplatz.Core.ApiClient.Generated.RemoveBlockedHttpRequest
            {
                PropertyId = propertyId
            }
        );

        return (result.Result?.Success == true, result.Result?.Message);
    }

    #endregion

    /// <summary>
    /// Navigates to property details
    /// </summary>
    [RelayCommand]
    private async Task ViewPropertyDetailsAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Navigate to property details page (to be implemented)
        Logger.LogInformation("[Blocked] Navigating to property details for ID: {PropertyId}", property.Id);
        // TODO: Implement navigation to property details page
    }
}
