using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Wrapper for PropertyListItemDto that tracks selection state
/// </summary>
public partial class SelectablePropertyItem : ObservableObject
{
    public PropertyListItemDto Property { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SelectablePropertyItem(PropertyListItemDto property)
    {
        Property = property;
    }
}

/// <summary>
/// ViewModel for BlockedPage - manages user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// Extends PropertyCollectionViewModelBase for shared collection functionality.
/// Supports bulk unblock via selection mode.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class BlockedViewModel : PropertyCollectionViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionModeButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowBulkUnblockButton))]
    [NotifyPropertyChangedFor(nameof(IsNotSelectionMode))]
    private bool _isSelectionMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBulkUnblockButton))]
    [NotifyPropertyChangedFor(nameof(SelectedCountText))]
    private int _selectedCount;

    public string SelectionModeButtonText => IsSelectionMode ? "Abbrechen" : "Auswählen";
    public bool ShowBulkUnblockButton => IsSelectionMode && SelectedCount > 0;
    public bool IsNotSelectionMode => !IsSelectionMode;
    public string SelectedCountText => $"{SelectedCount} ausgewählt";

    public BlockedViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<BlockedViewModel> logger)
        : base(authService, mediator, navigator, logger)
    {
        // Header setup is now automatic via PageNavigatedEvent from BasePage
    }

    /// <summary>
    /// Gets currently selected properties
    /// </summary>
    private IEnumerable<PropertyListItemDto> GetSelectedProperties()
    {
        return Properties.Where(p => IsPropertySelected(p));
    }

    /// <summary>
    /// Toggles selection mode on/off
    /// </summary>
    [RelayCommand]
    private void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// Clears all selections
    /// </summary>
    private void ClearSelection()
    {
        _selectedPropertyIds.Clear();
        SelectedCount = 0;
    }

    // Track selected property IDs for efficient lookup
    private readonly HashSet<Guid> _selectedPropertyIds = new();

    /// <summary>
    /// Toggles selection state of a property
    /// </summary>
    [RelayCommand]
    private void TogglePropertySelection(PropertyListItemDto property)
    {
        if (property == null) return;

        if (_selectedPropertyIds.Contains(property.Id))
        {
            _selectedPropertyIds.Remove(property.Id);
        }
        else
        {
            _selectedPropertyIds.Add(property.Id);
        }

        SelectedCount = _selectedPropertyIds.Count;
    }

    /// <summary>
    /// Checks if a property is selected
    /// </summary>
    public bool IsPropertySelected(PropertyListItemDto property)
    {
        return _selectedPropertyIds.Contains(property.Id);
    }

    /// <summary>
    /// Unblocks all selected properties
    /// </summary>
    [RelayCommand]
    private async Task BulkUnblockAsync()
    {
        var selectedProperties = GetSelectedProperties().ToList();
        if (selectedProperties.Count == 0) return;

        var count = selectedProperties.Count;
        var confirmed = await ShowBulkUnblockConfirmationAsync(count);
        if (!confirmed) return;

        IsBusy = true;
        BusyMessage = $"Hebe Blockierung von {count} Immobilien auf...";

        var successCount = 0;
        var failedCount = 0;

        try
        {
            foreach (var property in selectedProperties)
            {
                try
                {
                    var (success, _) = await RemovePropertyFromApiAsync(property.Id);
                    if (success)
                    {
                        Properties.Remove(property);
                        _selectedPropertyIds.Remove(property.Id);
                        successCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch
                {
                    failedCount++;
                }
            }

            IsEmpty = !Properties.Any();
            IsSelectionMode = false;
            ClearSelection();

            if (failedCount == 0)
            {
                await ShowSuccessDialogAsync(
                    "Blockierungen aufgehoben",
                    $"{successCount} Immobilie(n) wurden erfolgreich entblockt.");
            }
            else
            {
                await ShowSuccessDialogAsync(
                    "Teilweise erfolgreich",
                    $"{successCount} Immobilie(n) entblockt, {failedCount} fehlgeschlagen.");
            }
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private async Task<bool> ShowBulkUnblockConfirmationAsync(int count)
    {
        var dialog = new ContentDialog
        {
            Title = "Blockierungen aufheben?",
            Content = $"Möchten Sie die Blockierung von {count} Immobilie(n) wirklich aufheben? Diese werden wieder in der Hauptliste angezeigt.",
            PrimaryButtonText = "Alle entblocken",
            SecondaryButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
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
            CreatedAt: prop.CreatedAt.DateTime,
            InquiryType: (InquiryType)prop.InquiryType
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

        Logger.LogInformation("[Blocked] Navigating to property details for ID: {PropertyId}", property.Id);
        await Navigator.NavigateRouteAsync(this, "PropertyDetail", data: new PropertyDetailData(property.Id));
    }
}
