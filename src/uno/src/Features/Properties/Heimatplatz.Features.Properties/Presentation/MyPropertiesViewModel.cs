using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Collections;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for MyPropertiesPage - manages user's own properties
/// Uses PaginatedObservableCollection for automatic incremental loading.
/// Implements INavigationAware for automatic lifecycle handling via BasePage
/// Implements IPageInfo for header integration
/// Registered via Uno.Extensions.Navigation ViewMap (not [Service] attribute)
/// </summary>
public partial class MyPropertiesViewModel : ObservableObject, INavigationAware, IPageInfo
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<MyPropertiesViewModel> _logger;

    private const int PageSize = 20;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    // Paginated collection for automatic incremental loading
    private PaginatedObservableCollection<PropertyListItemDto>? _properties;
    public PaginatedObservableCollection<PropertyListItemDto> Properties =>
        _properties ??= CreatePaginatedCollection();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    private bool _isEmpty;

    public bool IsNotEmpty => !IsEmpty;

    #region IPageInfo Implementation

    public PageType PageType => PageType.List;
    public string PageTitle => "Meine Immobilien";
    public Type? MainHeaderViewModel => null;

    #endregion

    public MyPropertiesViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<MyPropertiesViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _logger = logger;

        // Initialize as empty until properties are loaded
        IsEmpty = true;

        // Subscribe to authentication state changes to reload data when user logs in/out
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        _logger.LogInformation("[MyProperties] ViewModel constructed");

        // Load properties immediately if user is authenticated
        if (_authService.IsAuthenticated)
        {
            _ = Properties.ResetAndReloadAsync();
        }
    }

    /// <summary>
    /// Creates the paginated collection with the load function
    /// </summary>
    private PaginatedObservableCollection<PropertyListItemDto> CreatePaginatedCollection()
    {
        return new PaginatedObservableCollection<PropertyListItemDto>(
            LoadPageAsync,
            pageSize: PageSize
        );
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[MyProperties] Auth state changed. IsAuthenticated: {IsAuthenticated}", isAuthenticated);
        if (isAuthenticated)
        {
            // Reload properties when user logs in
            await Properties.ResetAndReloadAsync();
        }
        else
        {
            // Clear properties when user logs out
            Properties.Clear();
            IsEmpty = true;
        }
    }

    #region INavigationAware Implementation

    /// <summary>
    /// Called by BasePage when navigated to (via INavigationAware).
    /// Header setup is automatic via PageNavigatedEvent from BasePage.
    /// </summary>
    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogInformation("[MyProperties] OnNavigatedTo called");
        // Always reload to pick up newly created/edited properties
        _ = Properties.ResetAndReloadAsync();
    }

    /// <summary>
    /// Called by BasePage when navigated from (via INavigationAware)
    /// </summary>
    public void OnNavigatedFrom()
    {
        _logger.LogInformation("[MyProperties] OnNavigatedFrom called");
    }

    #endregion

    /// <summary>
    /// Called when the page is navigated to (legacy - prefer INavigationAware)
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        _logger.LogInformation("[MyProperties] OnNavigatedToAsync called");
        // Header setup is now automatic via PageNavigatedEvent from BasePage
        await Properties.ResetAndReloadAsync();
    }

    /// <summary>
    /// Loads a page of properties - called by PaginatedObservableCollection
    /// </summary>
    private async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> LoadPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        _logger.LogInformation("[MyProperties] Loading page {Page} with pageSize {PageSize}", page, pageSize);

        try
        {
            var (context, response) = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserPropertiesHttpRequest
                {
                    Page = page,
                    PageSize = pageSize
                },
                ct
            );

            if (response?.Properties == null)
            {
                _logger.LogInformation("[MyProperties] Response was null or empty");
                if (page == 0) IsEmpty = true;
                return (Enumerable.Empty<PropertyListItemDto>(), false, 0);
            }

            _logger.LogInformation("[MyProperties] Page {Page} loaded. Items: {Count}, HasMore: {HasMore}",
                page, response.Properties.Count, response.HasMore);

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

            // Update IsEmpty based on whether we have any items
            if (page == 0)
            {
                IsEmpty = response.Properties.Count == 0;
            }
            else if (response.Properties.Count > 0)
            {
                IsEmpty = false;
            }

            return (items, response.HasMore, response.Total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MyProperties] Error loading page {Page}", page);
            _ = ShowErrorDialogAsync("Fehler beim Laden", $"Die Immobilien konnten nicht geladen werden: {ex.Message}");
            return (Enumerable.Empty<PropertyListItemDto>(), false, 0);
        }
    }

    /// <summary>
    /// Navigates to AddPropertyPage to create a new property
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAddPropertyAsync()
    {
        await _navigator.NavigateViewModelAsync<AddPropertyViewModel>(this);
    }

    /// <summary>
    /// Navigates to EditPropertyPage to edit the selected property
    /// </summary>
    [RelayCommand]
    private async Task EditPropertyAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Navigate to EditPropertyPage with the property ID
        await _navigator.NavigateViewModelAsync<EditPropertyViewModel>(
            this,
            data: new EditPropertyData(property.Id)
        );
    }

    /// <summary>
    /// Deletes the selected property after user confirmation
    /// </summary>
    [RelayCommand]
    private async Task DeletePropertyAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Show confirmation dialog
        var confirmed = await ShowDeleteConfirmationAsync(property);
        if (!confirmed) return;

        IsBusy = true;
        BusyMessage = "Lösche Immobilie...";

        try
        {
            // Der DeletePropertyHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var result = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.DeletePropertyHttpRequest
                {
                    Id = property.Id
                }
            );

            if (result.Result?.Success == true)
            {
                // Remove from collection
                Properties.Remove(property);
                IsEmpty = !Properties.Any();

                // Show success message
                await ShowSuccessDialogAsync("Erfolgreich gelöscht", result.Result.Message ?? "Die Immobilie wurde erfolgreich gelöscht.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("Fehler beim Löschen", $"Die Immobilie konnte nicht gelöscht werden: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Shows a confirmation dialog for deleting a property
    /// </summary>
    private async Task<bool> ShowDeleteConfirmationAsync(PropertyListItemDto property)
    {
        var dialog = new ContentDialog
        {
            Title = "Immobilie löschen?",
            Content = $"Möchten Sie \"{property.Title}\" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.",
            PrimaryButtonText = "Löschen",
            SecondaryButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows a success dialog
    /// </summary>
    private async Task ShowSuccessDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an error dialog
    /// </summary>
    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Window.Current?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
