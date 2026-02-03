using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for FavoritesPage - manages user's favorited properties.
/// Extends PropertyCollectionViewModelBase for shared collection functionality.
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class FavoritesViewModel : PropertyCollectionViewModelBase
{
    public FavoritesViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<FavoritesViewModel> logger)
        : base(authService, mediator, navigator, logger)
    {
    }

    #region Abstract Property Implementations

    public override string PageTitle => "Meine Favoriten";
    public override string EmptyStateIcon => "\uE734"; // Heart icon
    public override string EmptyStateTitle => "Noch keine Favoriten";
    public override string EmptyStateDescription => "Markieren Sie Immobilien als Favoriten, um sie hier zu sehen.";
    protected override string LoadingMessage => "Lade Favoriten...";
    protected override string RemovingMessage => "Entferne Favorit...";
    protected override string RemoveConfirmTitle => "Favorit entfernen?";
    protected override string RemoveSuccessTitle => "Erfolgreich entfernt";
    protected override string RemoveErrorTitle => "Fehler beim Entfernen";
    protected override string LoadErrorTitle => "Fehler beim Laden";

    protected override string GetRemoveConfirmMessage(PropertyListItemDto property)
        => $"Möchten Sie \"{property.Title}\" wirklich aus Ihren Favoriten entfernen?";

    protected override string GetRemoveSuccessMessage(string? apiMessage)
        => apiMessage ?? "Die Immobilie wurde aus den Favoriten entfernt.";

    protected override string GetRemoveErrorMessage(string errorDetails)
        => $"Die Immobilie konnte nicht aus den Favoriten entfernt werden: {errorDetails}";

    protected override string GetLoadErrorMessage(string errorDetails)
        => $"Die Favoriten konnten nicht geladen werden: {errorDetails}";

    #endregion

    #region Abstract Method Implementations

    protected override async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore)> FetchPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var (context, response) = await Mediator.Request(
            new Heimatplatz.Core.ApiClient.Generated.GetUserFavoritesHttpRequest
            {
                Page = page,
                PageSize = pageSize
            },
            ct
        );

        if (response?.Properties == null)
            return (Enumerable.Empty<PropertyListItemDto>(), false);

        var items = response.Properties.Select(prop => new PropertyListItemDto(
            Id: prop.Id,
            Title: prop.Title,
            Address: prop.Address,
            MunicipalityId: prop.MunicipalityId,
            City: prop.City,
            PostalCode: prop.PostalCode,
            Price: (decimal)prop.Price,
            LivingAreaM2: prop.LivingAreaM2,
            PlotAreaM2: prop.PlotAreaM2,
            Rooms: prop.Rooms,
            Type: Enum.Parse<PropertyType>(prop.Type.ToString()),
            SellerType: Enum.Parse<SellerType>(prop.SellerType.ToString()),
            SellerName: prop.SellerName,
            ImageUrls: prop.ImageUrls,
            CreatedAt: prop.CreatedAt.DateTime,
            InquiryType: Enum.Parse<InquiryType>(prop.InquiryType.ToString())
        ));

        return (items, response.HasMore);
    }

    protected override async Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId)
    {
        var result = await Mediator.Request(
            new Heimatplatz.Core.ApiClient.Generated.RemoveFavoriteHttpRequest
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

        Logger.LogInformation("[Favorites] Navigating to property details for ID: {PropertyId}", property.Id);

        // Navigate to ForeclosureDetail for foreclosure properties, PropertyDetail otherwise
        if (property.Type == PropertyType.Foreclosure)
        {
            await Navigator.NavigateRouteAsync(this, "ForeclosureDetail", data: new ForeclosureDetailData(property.Id));
        }
        else
        {
            await Navigator.NavigateRouteAsync(this, "PropertyDetail", data: new PropertyDetailData(property.Id));
        }
    }
}
