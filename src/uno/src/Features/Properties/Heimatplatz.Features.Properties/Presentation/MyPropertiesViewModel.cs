using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
/// ViewModel for MyPropertiesPage - manages user's own properties
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class MyPropertiesViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<MyPropertiesViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private ObservableCollection<PropertyListItemDto> _properties = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    private bool _isEmpty;

    public bool IsNotEmpty => !IsEmpty;

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

    /// <summary>
    /// Called when the page is navigated to - must be called manually from Page.OnNavigatedTo
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        _logger.LogInformation("[MyProperties] OnNavigatedToAsync called");
        await LoadPropertiesAsync();
    }

    /// <summary>
    /// Loads all properties for the current user
    /// </summary>
    private async Task LoadPropertiesAsync()
    {
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

            // Clear existing properties
            Properties.Clear();

            // Update empty state immediately after clearing
            IsEmpty = response?.Properties == null || !response.Properties.Any();
            _logger.LogInformation("[MyProperties] IsEmpty set to: {IsEmpty}", IsEmpty);

            if (response?.Properties != null)
            {
                _logger.LogInformation("[MyProperties] Adding {Count} properties to collection", response.Properties.Count);
                foreach (var prop in response.Properties)
                {
                    Properties.Add(new PropertyListItemDto(
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
                    ));
                }
                _logger.LogInformation("[MyProperties] Final Properties.Count: {Count}", Properties.Count);
            }
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
