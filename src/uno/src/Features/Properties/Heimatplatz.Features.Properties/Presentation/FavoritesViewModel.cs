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
/// ViewModel for FavoritesPage - manages user's favorited properties
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class FavoritesViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<FavoritesViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private ObservableCollection<PropertyListItemDto> _favorites = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    private bool _isEmpty;

    public bool IsNotEmpty => !IsEmpty;

    public FavoritesViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger<FavoritesViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _logger = logger;

        // Initialize as empty until favorites are loaded
        IsEmpty = true;

        // Subscribe to authentication state changes
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        _logger.LogInformation("[Favorites] ViewModel constructed");

        // Load favorites immediately if user is authenticated
        if (_authService.IsAuthenticated)
        {
            _ = LoadFavoritesAsync();
        }
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        _logger.LogInformation("[Favorites] Auth state changed. IsAuthenticated: {IsAuthenticated}", isAuthenticated);
        if (isAuthenticated)
        {
            await LoadFavoritesAsync();
        }
        else
        {
            Favorites.Clear();
            IsEmpty = true;
        }
    }

    /// <summary>
    /// Called when the page is navigated to
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        _logger.LogInformation("[Favorites] OnNavigatedToAsync called");
        await LoadFavoritesAsync();
    }

    /// <summary>
    /// Loads all favorited properties for the current user
    /// </summary>
    private async Task LoadFavoritesAsync()
    {
        IsBusy = true;
        BusyMessage = "Lade Favoriten...";

        try
        {
            _logger.LogInformation("[Favorites] Starting to load favorites");

            var (context, response) = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserFavoritesHttpRequest()
            );

            _logger.LogInformation("[Favorites] Response received. Properties count: {Count}", response?.Properties?.Count ?? 0);

            Favorites.Clear();

            IsEmpty = response?.Properties == null || !response.Properties.Any();
            _logger.LogInformation("[Favorites] IsEmpty set to: {IsEmpty}", IsEmpty);

            if (response?.Properties != null)
            {
                _logger.LogInformation("[Favorites] Adding {Count} favorites to collection", response.Properties.Count);
                foreach (var prop in response.Properties)
                {
                    Favorites.Add(new PropertyListItemDto(
                        Id: prop.Id,
                        Titel: prop.Titel,
                        Adresse: prop.Adresse,
                        Ort: prop.Ort,
                        Preis: (decimal)prop.Preis,
                        WohnflaecheM2: prop.WohnflaecheM2,
                        GrundstuecksflaecheM2: prop.GrundstuecksflaecheM2,
                        Zimmer: prop.Zimmer,
                        Typ: (PropertyType)prop.Typ,
                        AnbieterTyp: (SellerType)prop.AnbieterTyp,
                        AnbieterName: prop.AnbieterName,
                        BildUrls: prop.BildUrls
                    ));
                }
                _logger.LogInformation("[Favorites] Final Favorites.Count: {Count}", Favorites.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Favorites] Error loading favorites");
            await ShowErrorDialogAsync("Fehler beim Laden", $"Die Favoriten konnten nicht geladen werden: {ex.Message}");
            IsEmpty = true;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Removes a property from favorites
    /// </summary>
    [RelayCommand]
    private async Task RemoveFavoriteAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Show confirmation dialog
        var confirmed = await ShowRemoveFavoriteConfirmationAsync(property);
        if (!confirmed) return;

        IsBusy = true;
        BusyMessage = "Entferne Favorit...";

        try
        {
            var result = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.RemoveFavoriteHttpRequest
                {
                    PropertyId = property.Id
                }
            );

            if (result.Result?.Success == true)
            {
                Favorites.Remove(property);
                IsEmpty = !Favorites.Any();

                await ShowSuccessDialogAsync("Erfolgreich entfernt", result.Result.Message ?? "Die Immobilie wurde aus den Favoriten entfernt.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("Fehler beim Entfernen", $"Die Immobilie konnte nicht aus den Favoriten entfernt werden: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Navigates to property details
    /// </summary>
    [RelayCommand]
    private async Task ViewPropertyDetailsAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Navigate to property details page (to be implemented)
        _logger.LogInformation("[Favorites] Navigating to property details for ID: {PropertyId}", property.Id);
        // TODO: Implement navigation to property details page
    }

    /// <summary>
    /// Shows a confirmation dialog for removing a favorite
    /// </summary>
    private async Task<bool> ShowRemoveFavoriteConfirmationAsync(PropertyListItemDto property)
    {
        var dialog = new ContentDialog
        {
            Title = "Favorit entfernen?",
            Content = $"Möchten Sie \"{property.Titel}\" wirklich aus Ihren Favoriten entfernen?",
            PrimaryButtonText = "Entfernen",
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
