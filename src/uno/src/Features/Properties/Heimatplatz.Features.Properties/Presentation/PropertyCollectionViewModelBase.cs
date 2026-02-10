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
/// Base ViewModel for property collection pages (Favorites, Blocked).
/// Provides common functionality for loading, displaying, and removing properties from collections.
/// Uses PaginatedObservableCollection for automatic incremental loading.
/// Implements INavigationAware for automatic lifecycle handling via BasePage.
/// Implements IPageInfo for header integration.
/// </summary>
public abstract partial class PropertyCollectionViewModelBase : ObservableObject, INavigationAware, IPageInfo
{
    protected readonly IAuthService AuthService;
    protected readonly IMediator Mediator;
    protected readonly INavigator Navigator;
    protected readonly ILogger Logger;

    protected const int PageSize = 20;

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
    private bool _isEmpty = true;

    public bool IsNotEmpty => !IsEmpty;

    // IPageInfo implementation - List pages show hamburger button
    public virtual PageType PageType => PageType.List;
    public virtual Type? MainHeaderViewModel => null;

    // Abstract properties for UI texts (to be implemented by derived classes)
    public abstract string PageTitle { get; }
    public abstract string EmptyStateIcon { get; }
    public abstract string EmptyStateTitle { get; }
    public abstract string EmptyStateDescription { get; }
    protected abstract string LoadingMessage { get; }
    protected abstract string RemovingMessage { get; }
    protected abstract string RemoveConfirmTitle { get; }
    protected abstract string RemoveSuccessTitle { get; }
    protected abstract string RemoveErrorTitle { get; }
    protected abstract string LoadErrorTitle { get; }

    /// <summary>
    /// Gets the confirmation message for removing a property
    /// </summary>
    protected abstract string GetRemoveConfirmMessage(PropertyListItemDto property);

    /// <summary>
    /// Gets the success message after removing a property
    /// </summary>
    protected abstract string GetRemoveSuccessMessage(string? apiMessage);

    /// <summary>
    /// Gets the error message when removing fails
    /// </summary>
    protected abstract string GetRemoveErrorMessage(string errorDetails);

    /// <summary>
    /// Gets the error message when loading fails
    /// </summary>
    protected abstract string GetLoadErrorMessage(string errorDetails);

    protected PropertyCollectionViewModelBase(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILogger logger)
    {
        AuthService = authService;
        Mediator = mediator;
        Navigator = navigator;
        Logger = logger;

        // Subscribe to authentication state changes
        AuthService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        Logger.LogInformation("[{PageTitle}] ViewModel constructed", PageTitle);

        // Load properties immediately if user is authenticated
        if (AuthService.IsAuthenticated)
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
        Logger.LogInformation("[{PageTitle}] Auth state changed. IsAuthenticated: {IsAuthenticated}", PageTitle, isAuthenticated);
        if (isAuthenticated)
        {
            await Properties.ResetAndReloadAsync();
        }
        else
        {
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
        Logger.LogInformation("[{PageTitle}] OnNavigatedTo called", PageTitle);
        // Skip reload if we already have data — avoids triggering
        // CollectionChanged on both old and new page instances (StackOverflow).
        if (Properties.Count == 0)
        {
            _ = Properties.ResetAndReloadAsync();
        }
    }

    /// <summary>
    /// Called by BasePage when navigated from (via INavigationAware)
    /// </summary>
    public void OnNavigatedFrom()
    {
        Logger.LogInformation("[{PageTitle}] OnNavigatedFrom called", PageTitle);
    }

    #endregion

    /// <summary>
    /// Called when the page is navigated to (legacy - prefer INavigationAware)
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        Logger.LogInformation("[{PageTitle}] OnNavigatedToAsync called", PageTitle);
        // Header setup is now automatic via PageNavigatedEvent from BasePage
        await Properties.ResetAndReloadAsync();
    }

    /// <summary>
    /// Fetches a page of properties from the API. To be implemented by derived classes.
    /// </summary>
    /// <returns>Tuple with items, hasMore flag, and total count</returns>
    protected abstract Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Removes a property from the collection via API. To be implemented by derived classes.
    /// </summary>
    /// <returns>Tuple with success status and optional message</returns>
    protected abstract Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId);

    /// <summary>
    /// Loads a page of properties - called by PaginatedObservableCollection
    /// </summary>
    private async Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> LoadPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        Logger.LogInformation("[{PageTitle}] Loading page {Page} with pageSize {PageSize}", PageTitle, page, pageSize);

        try
        {
            var (items, hasMore, totalCount) = await FetchPageAsync(page, pageSize, ct);
            var itemsList = items.ToList();

            Logger.LogInformation("[{PageTitle}] Page {Page} loaded. Items: {Count}, HasMore: {HasMore}",
                PageTitle, page, itemsList.Count, hasMore);

            // Update IsEmpty based on whether we have any items at all
            if (page == 0)
            {
                IsEmpty = itemsList.Count == 0;
            }
            else if (itemsList.Count > 0)
            {
                IsEmpty = false;
            }

            return (itemsList, hasMore, totalCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{PageTitle}] Error loading page {Page}", PageTitle, page);
            // Show error dialog on UI thread
            _ = ShowErrorDialogAsync(LoadErrorTitle, GetLoadErrorMessage(ex.Message));
            return (Enumerable.Empty<PropertyListItemDto>(), false, 0);
        }
    }

    /// <summary>
    /// Removes a property from the collection
    /// </summary>
    [RelayCommand]
    private async Task RemoveFromCollectionAsync(PropertyListItemDto property)
    {
        if (property == null) return;

        // Show confirmation dialog
        var confirmed = await ShowRemoveConfirmationAsync(property);
        if (!confirmed) return;

        IsBusy = true;
        BusyMessage = RemovingMessage;

        try
        {
            var (success, message) = await RemovePropertyFromApiAsync(property.Id);

            if (success)
            {
                Properties.Remove(property);
                IsEmpty = !Properties.Any();
                // Success dialog removed per user request - immediate feedback through UI update is sufficient
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(RemoveErrorTitle, GetRemoveErrorMessage(ex.Message));
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Shows a confirmation dialog for removing a property
    /// </summary>
    private async Task<bool> ShowRemoveConfirmationAsync(PropertyListItemDto property)
    {
        var dialog = new ContentDialog
        {
            Title = RemoveConfirmTitle,
            Content = GetRemoveConfirmMessage(property),
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
    protected async Task ShowSuccessDialogAsync(string title, string message)
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
    protected async Task ShowErrorDialogAsync(string title, string message)
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
