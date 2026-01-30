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

    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    // Stable collection reference — never replace, only update contents in-place.
    // Replacing the reference triggers OnPropertyChanged("Properties") which causes
    // ItemsRepeater.OnItemsSourceChanged on both old and new page instances (StackOverflow).
    private readonly BatchObservableCollection<PropertyListItemDto> _properties = new();
    public BatchObservableCollection<PropertyListItemDto> Properties => _properties;

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
            _ = LoadPropertiesAsync();
        }
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[MyProperties] Auth state changed. IsAuthenticated: {IsAuthenticated}", isAuthenticated);
        if (isAuthenticated)
        {
            // Reload properties when user logs in
            await LoadPropertiesAsync();
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
        // Skip reload if we already have data — avoids _properties.Reset() triggering
        // CollectionChanged on both old and new page instances (StackOverflow).
        if (Properties.Count == 0)
        {
            _ = LoadPropertiesAsync();
        }
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
        await LoadPropertiesAsync();
    }

    /// <summary>
    /// Loads all properties for the current user
    /// </summary>
    private async Task LoadPropertiesAsync()
    {
        if (_isLoading)
        {
            _logger.LogInformation("[MyProperties] LoadPropertiesAsync skipped - already loading");
            return;
        }

        _isLoading = true;
        IsBusy = true;
        BusyMessage = "Lade Immobilien...";

        try
        {
            _logger.LogInformation("[MyProperties] Starting to load properties");

            // Der GetUserPropertiesHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var (context, response) = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserPropertiesHttpRequest()
            );

            _logger.LogInformation("[MyProperties] Response received. Response null: {IsNull}", response == null);
            _logger.LogInformation("[MyProperties] Properties null: {IsNull}", response?.Properties == null);
            _logger.LogInformation("[MyProperties] Properties count: {Count}", response?.Properties?.Count ?? 0);

            // Build new list to replace collection atomically
            var newProperties = new List<PropertyListItemDto>();

            if (response?.Properties != null)
            {
                _logger.LogInformation("[MyProperties] Mapping {Count} properties", response.Properties.Count);
                foreach (var prop in response.Properties)
                {
                    newProperties.Add(new PropertyListItemDto(
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
                }
            }

            // Batch-replace all items with a single Reset notification to avoid StackOverflow
            _properties.Reset(newProperties);
            IsEmpty = newProperties.Count == 0;
            _logger.LogInformation("[MyProperties] Properties replaced. Count: {Count}, IsEmpty: {IsEmpty}", newProperties.Count, IsEmpty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MyProperties] Error loading properties");
            await ShowErrorDialogAsync("Fehler beim Laden", $"Die Immobilien konnten nicht geladen werden: {ex.Message}");
            IsEmpty = true;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
            _isLoading = false;
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
