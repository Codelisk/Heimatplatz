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
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
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

    /// <summary>
    /// Fehlermeldung wenn das Laden fehlschlaegt. Wird inline im Leer-Zustand angezeigt
    /// (mit Retry-Button) statt als modaler Dialog - ein Dialog blockiert alle Eingaben
    /// und laesst das Busy-Overlay bis zum OK-Tap stehen.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    [NotifyPropertyChangedFor(nameof(ShowLoadError))]
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
    public partial string? LoadErrorMessage { get; set; }

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    /// <summary>
    /// Lade-Fehler nur anzeigen solange der Benutzer angemeldet ist - laeuft die
    /// Session waehrend des Ladens ab (Logout durch Token-Refresh-Fehler), wuerden
    /// sonst Anmelde-Hinweis und Fehler-Zustand uebereinander liegen.
    /// </summary>
    public bool ShowLoadError => HasLoadError && IsLoggedIn;

    /// <summary>
    /// Regulaerer Leer-Zustand: nur wenn angemeldet, kein Lade-Fehler ansteht und
    /// gerade nicht geladen wird (sonst wuerden Leer-, Fehler- bzw. Lade-Zustand
    /// gleichzeitig angezeigt).
    /// </summary>
    public bool ShowRegularEmptyState => IsLoggedIn && !HasLoadError && !IsBusy;

    /// <summary>
    /// True wenn kein Benutzer angemeldet ist - die Seiten zeigen dann statt des
    /// regulaeren Leer-Zustands einen Anmelde-Hinweis mit Login-Button.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowLoadError))]
    public partial bool IsLoggedOut { get; set; }

    /// <summary>
    /// True wenn der Benutzer angemeldet ist, die Seite aber die Verkaeufer-Rolle
    /// erfordert und das Konto sie nicht hat (verhindert 403-Fehlerdialoge).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    [NotifyPropertyChangedFor(nameof(ShowRegularEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowLoadError))]
    public partial bool IsSellerBlocked { get; set; }

    public bool IsLoggedIn => !IsLoggedOut && !IsSellerBlocked;

    /// <summary>
    /// Ob die Seite die Verkaeufer-Rolle erfordert (z.B. Meine Immobilien)
    /// </summary>
    protected virtual bool RequiresSellerRole => false;

    // Abstrakte Texte (von abgeleiteten Klassen zu implementieren)
    protected abstract string LoadingMessage { get; }
    protected abstract string RemovingMessage { get; }
    protected abstract string RemoveConfirmTitle { get; }
    protected abstract string RemoveErrorTitle { get; }

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
        IsLoggedOut = !AuthService.IsAuthenticated;

        AuthService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoggedOut = !isAuthenticated;
            IsSellerBlocked = isAuthenticated && RequiresSellerRole && !AuthService.IsSeller;

            if (isAuthenticated && !IsSellerBlocked)
            {
                _ = ReloadAsync();
            }
            else
            {
                Properties.Clear();
                IsEmpty = true;
                LoadErrorMessage = null;
            }
        });
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        IsLoggedOut = !AuthService.IsAuthenticated;
        IsSellerBlocked = !IsLoggedOut && RequiresSellerRole && !AuthService.IsSeller;

        if (IsLoggedOut || IsSellerBlocked)
        {
            Properties.Clear();
            IsEmpty = true;
            LoadErrorMessage = null;
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
        int page, int pageSize, bool forceRemoteRefresh, CancellationToken ct);

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
        LoadErrorMessage = null;
        try
        {
            _currentPage = 0;
            var items = await LoadPageSafeAsync(0, forceRemoteRefresh: false);

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
    /// Erneut versuchen nach fehlgeschlagenem Laden (Inline-Fehlerzustand)
    /// </summary>
    [RelayCommand]
    private Task RetryLoadAsync() => ReloadAsync();

    /// <summary>
    /// Pull-to-Refresh
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            LoadErrorMessage = null;
            _currentPage = 0;
            var items = await LoadPageSafeAsync(0, forceRemoteRefresh: true);

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
            var items = await LoadPageSafeAsync(_currentPage + 1, forceRemoteRefresh: false);
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

    private async Task<List<PropertyListItemDto>> LoadPageSafeAsync(int page, bool forceRemoteRefresh)
    {
        Logger.LogInformation("[{Type}] Loading page {Page} with pageSize {PageSize}", GetType().Name, page, PageSize);

        try
        {
            var (items, hasMore, totalCount) = await FetchPageAsync(
                page,
                PageSize,
                forceRemoteRefresh,
                CancellationToken.None);
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

            // Kein modaler Dialog (blockiert Eingaben, haelt das Busy-Overlay fest) -
            // Inline-Fehlerzustand mit Retry; Folgeseiten-Fehler (Infinite Scroll) bleiben still.
            if (page == 0)
            {
                // Keine rohen HTTP-/Exception-Texte - benutzerfreundliche Meldung
                var hint = ex is HttpRequestException
                    ? "Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut."
                    : "Bitte versuchen Sie es später erneut.";
                LoadErrorMessage = GetLoadErrorMessage(hint);
            }
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
            Logger.LogError(ex, "[{Type}] Error removing property {PropertyId}", GetType().Name, property.Id);
            var hint = ex is HttpRequestException
                ? "Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut."
                : "Bitte versuchen Sie es später erneut.";
            await Dialogs.Alert(RemoveErrorTitle, GetRemoveErrorMessage(hint));
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Navigiert zur Login-Seite (aus dem Anmelde-Hinweis im Leer-Zustand)
    /// </summary>
    [RelayCommand]
    private Task GoToLoginAsync() => Navigator.NavigateTo("Login", relativeNavigation: false);

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
