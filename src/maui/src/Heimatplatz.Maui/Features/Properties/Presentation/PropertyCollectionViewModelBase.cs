using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Basis-ViewModel fuer Immobilien-Sammlungsseiten (Favoriten, Blockierte, Meine Immobilien).
/// Bietet gemeinsame Funktionalitaet fuer Laden (mit Infinite Scroll), Anzeigen
/// und Entfernen von Immobilien aus Sammlungen.
/// </summary>
public abstract partial class PropertyCollectionViewModelBase : ObservableObject, IPageLifecycleAware, IDisposable
{
    protected const int PageSize = 20;

    protected IAuthService AuthService { get; }
    protected IMediator Mediator { get; }
    protected INavigator Navigator { get; }
    protected IDialogs Dialogs { get; }
    protected ILogger Logger { get; }

    private int _currentPage;
    private bool _hasMore;

    public ObservableCollection<PropertyListItemDto> Properties { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    public partial bool IsEmpty { get; set; }

    public bool IsNotEmpty => !IsEmpty;

    // Abstrakte Texte (von abgeleiteten Klassen zu implementieren)
    protected abstract string LoadingMessage { get; }
    protected abstract string RemovingMessage { get; }
    protected abstract string RemoveConfirmTitle { get; }
    protected abstract string RemoveErrorTitle { get; }
    protected abstract string LoadErrorTitle { get; }

    /// <summary>
    /// Bestaetigungstext fuer das Entfernen einer Immobilie
    /// </summary>
    protected abstract string GetRemoveConfirmMessage(PropertyListItemDto property);

    /// <summary>
    /// Fehlermeldung wenn das Entfernen fehlschlaegt
    /// </summary>
    protected abstract string GetRemoveErrorMessage(string errorDetails);

    /// <summary>
    /// Fehlermeldung wenn das Laden fehlschlaegt
    /// </summary>
    protected abstract string GetLoadErrorMessage(string errorDetails);

    /// <summary>
    /// Ob beim OnAppearing immer neu geladen wird (statt nur bei leerer Liste)
    /// </summary>
    protected virtual bool AlwaysReloadOnAppearing => false;

    protected PropertyCollectionViewModelBase(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        IDialogs dialogs,
        ILogger logger)
    {
        AuthService = authService;
        Mediator = mediator;
        Navigator = navigator;
        Dialogs = dialogs;
        Logger = logger;

        IsEmpty = true;

        AuthService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isAuthenticated)
            {
                _ = ReloadAsync();
            }
            else
            {
                Properties.Clear();
                IsEmpty = true;
            }
        });
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        if (!AuthService.IsAuthenticated)
        {
            Properties.Clear();
            IsEmpty = true;
            return;
        }

        if (AlwaysReloadOnAppearing || Properties.Count == 0)
        {
            _ = ReloadAsync();
        }
    }

    public void OnDisappearing()
    {
    }

    #endregion

    /// <summary>
    /// Ruft eine Seite der Immobilien von der API ab. Von abgeleiteten Klassen zu implementieren.
    /// </summary>
    protected abstract Task<(IEnumerable<PropertyListItemDto> Items, bool HasMore, int TotalCount)> FetchPageAsync(
        int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Entfernt eine Immobilie via API aus der Sammlung. Von abgeleiteten Klassen zu implementieren.
    /// </summary>
    protected abstract Task<(bool Success, string? Message)> RemovePropertyFromApiAsync(Guid propertyId);

    /// <summary>
    /// Laedt die Liste neu (erste Seite)
    /// </summary>
    protected async Task ReloadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        BusyMessage = LoadingMessage;
        try
        {
            _currentPage = 0;
            var items = await LoadPageSafeAsync(0);

            Properties.Clear();
            foreach (var item in items)
                Properties.Add(item);

            IsEmpty = Properties.Count == 0;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Pull-to-Refresh
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            _currentPage = 0;
            var items = await LoadPageSafeAsync(0);

            Properties.Clear();
            foreach (var item in items)
                Properties.Add(item);

            IsEmpty = Properties.Count == 0;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Naechste Seite laden (Infinite Scroll via RemainingItemsThreshold)
    /// </summary>
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsBusy || IsRefreshing || !_hasMore)
            return;

        IsLoadingMore = true;
        try
        {
            var items = await LoadPageSafeAsync(_currentPage + 1);
            if (items.Count > 0)
            {
                _currentPage++;
                foreach (var item in items)
                    Properties.Add(item);
            }
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task<List<PropertyListItemDto>> LoadPageSafeAsync(int page)
    {
        Logger.LogInformation("[{Type}] Loading page {Page} with pageSize {PageSize}", GetType().Name, page, PageSize);

        try
        {
            var (items, hasMore, totalCount) = await FetchPageAsync(page, PageSize, CancellationToken.None);
            _hasMore = hasMore;

            var itemsList = items.ToList();
            Logger.LogInformation("[{Type}] Page {Page} loaded. Items: {Count}, HasMore: {HasMore}, Total: {Total}",
                GetType().Name, page, itemsList.Count, hasMore, totalCount);

            return itemsList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Type}] Error loading page {Page}", GetType().Name, page);
            _hasMore = false;
            await Dialogs.Alert(LoadErrorTitle, GetLoadErrorMessage(ex.Message));
            return [];
        }
    }

    /// <summary>
    /// Entfernt eine Immobilie aus der Sammlung (mit Bestaetigung)
    /// </summary>
    [RelayCommand]
    private async Task RemoveFromCollectionAsync(PropertyListItemDto property)
    {
        var confirmed = await Dialogs.Confirm(RemoveConfirmTitle, GetRemoveConfirmMessage(property));
        if (!confirmed) return;

        IsBusy = true;
        BusyMessage = RemovingMessage;

        try
        {
            var (success, _) = await RemovePropertyFromApiAsync(property.Id);

            if (success)
            {
                Properties.Remove(property);
                IsEmpty = Properties.Count == 0;
            }
        }
        catch (Exception ex)
        {
            await Dialogs.Alert(RemoveErrorTitle, GetRemoveErrorMessage(ex.Message));
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Navigiert zur Detail-Seite (Zwangsversteigerungen zur ForeclosureDetailPage)
    /// </summary>
    [RelayCommand]
    private async Task PropertySelectedAsync(PropertyListItemDto property)
    {
        Logger.LogInformation("[{Type}] Navigating to details for {PropertyId}", GetType().Name, property.Id);

        if (property.Type == PropertyType.Foreclosure)
        {
            await Navigator.NavigateTo<ForeclosureDetailViewModel>(vm => vm.PropertyId = property.Id.ToString());
        }
        else
        {
            await Navigator.NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = property.Id.ToString());
        }
    }

    public virtual void Dispose()
    {
        AuthService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        GC.SuppressFinalize(this);
    }
}
