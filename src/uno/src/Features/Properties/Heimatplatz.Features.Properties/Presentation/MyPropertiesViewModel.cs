using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
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
        INavigator navigator)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;

        // Initialize as empty until properties are loaded
        IsEmpty = true;
    }

    /// <summary>
    /// Called when the page is navigated to
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
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
            // Der GetUserPropertiesHttpRequest wird automatisch aus der OpenAPI-Spec generiert
            var (context, response) = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserPropertiesHttpRequest()
            );

            Properties.Clear();

            if (response?.Properties != null)
            {
                foreach (var prop in response.Properties)
                {
                    Properties.Add(new PropertyListItemDto(
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
            }

            IsEmpty = !Properties.Any();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("Fehler beim Laden", $"Die Immobilien konnten nicht geladen werden: {ex.Message}");
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
    /// Navigates to AddPropertyPage in edit mode for the selected property
    /// </summary>
    [RelayCommand]
    private async Task EditPropertyAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Navigate to AddPropertyPage with the property ID for editing
        await _navigator.NavigateViewModelAsync<AddPropertyViewModel>(
            this,
            data: new Dictionary<string, object>
            {
                ["PropertyId"] = property.Id
            }
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
            Content = $"Möchten Sie \"{property.Titel}\" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.",
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
