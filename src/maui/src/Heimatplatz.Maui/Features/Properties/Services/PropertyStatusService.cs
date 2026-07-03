using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service zur Verwaltung von Favoriten- und Blockiert-Status der Immobilien des Benutzers.
/// Cached den Status lokal und synchronisiert mit der API.
/// </summary>
[Singleton]
public class PropertyStatusService : IPropertyStatusService
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly ILogger<PropertyStatusService> _logger;

    private readonly HashSet<Guid> _favoriteIds = new();
    private readonly HashSet<Guid> _blockedIds = new();
    private bool _isLoaded;
    private bool _isRefreshing;

    public event EventHandler? StatusChanged;

    public PropertyStatusService(
        IMediator mediator,
        IAuthService authService,
        ILogger<PropertyStatusService> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _logger = logger;

        // Cache leeren wenn Benutzer sich abmeldet
        _authService.AuthenticationStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            ClearCache();
        }
        else
        {
            // Status neu laden wenn Benutzer sich anmeldet
            _ = RefreshStatusAsync();
        }
    }

    public bool IsFavorite(Guid propertyId) => _favoriteIds.Contains(propertyId);

    public bool IsBlocked(Guid propertyId) => _blockedIds.Contains(propertyId);

    public async Task<bool> ToggleFavoriteAsync(Guid propertyId)
    {
        if (!_authService.IsAuthenticated)
        {
            _logger.LogWarning("[PropertyStatus] Cannot toggle favorite - user not authenticated");
            return false;
        }

        await EnsureLoadedAsync();

        var isFavorite = _favoriteIds.Contains(propertyId);

        try
        {
            if (isFavorite)
            {
                // Aus Favoriten entfernen
                var result = await _mediator.Request(
                    new RemoveFavoriteHttpRequest { PropertyId = propertyId });

                if (result.Result?.Success == true)
                {
                    _favoriteIds.Remove(propertyId);
                    _logger.LogInformation("[PropertyStatus] Removed favorite: {PropertyId}", propertyId);
                    StatusChanged?.Invoke(this, EventArgs.Empty);
                    return false;
                }
            }
            else
            {
                // Zu Favoriten hinzufuegen
                var result = await _mediator.Request(
                    new AddFavoriteHttpRequest
                    {
                        Body = new AddFavoriteRequest { PropertyId = propertyId }
                    });

                if (result.Result?.Success == true)
                {
                    _favoriteIds.Add(propertyId);
                    _logger.LogInformation("[PropertyStatus] Added favorite: {PropertyId}", propertyId);
                    StatusChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyStatus] Error toggling favorite for {PropertyId}", propertyId);
        }

        return isFavorite; // Bei Fehler urspruenglichen Status zurueckgeben
    }

    public async Task<bool> ToggleBlockedAsync(Guid propertyId)
    {
        if (!_authService.IsAuthenticated)
        {
            _logger.LogWarning("[PropertyStatus] Cannot toggle blocked - user not authenticated");
            return false;
        }

        await EnsureLoadedAsync();

        var isBlocked = _blockedIds.Contains(propertyId);

        try
        {
            if (isBlocked)
            {
                // Blockierung aufheben
                var result = await _mediator.Request(
                    new RemoveBlockedHttpRequest { PropertyId = propertyId });

                if (result.Result?.Success == true)
                {
                    _blockedIds.Remove(propertyId);
                    _logger.LogInformation("[PropertyStatus] Removed blocked: {PropertyId}", propertyId);
                    StatusChanged?.Invoke(this, EventArgs.Empty);
                    return false;
                }
            }
            else
            {
                // Blockieren
                var result = await _mediator.Request(
                    new AddBlockedHttpRequest
                    {
                        Body = new AddBlockedRequest { PropertyId = propertyId }
                    });

                if (result.Result?.Success == true)
                {
                    _blockedIds.Add(propertyId);
                    _logger.LogInformation("[PropertyStatus] Added blocked: {PropertyId}", propertyId);
                    StatusChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyStatus] Error toggling blocked for {PropertyId}", propertyId);
        }

        return isBlocked; // Bei Fehler urspruenglichen Status zurueckgeben
    }

    public async Task RefreshStatusAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            ClearCache();
            return;
        }

        // Reentrancy-Guard: verhindert rekursive Aufrufe aus StatusChanged-Handlern
        if (_isRefreshing)
        {
            _logger.LogInformation("[PropertyStatus] RefreshStatusAsync skipped - already refreshing");
            return;
        }

        _isRefreshing = true;
        try
        {
            _logger.LogInformation("[PropertyStatus] Loading user favorites and blocked...");

            // Favoriten laden
            var favoritesResult = await _mediator.Request(new GetUserFavoritesHttpRequest());

            _favoriteIds.Clear();
            if (favoritesResult.Result?.Properties != null)
            {
                foreach (var prop in favoritesResult.Result.Properties)
                {
                    _favoriteIds.Add(prop.Id);
                }
            }

            // Blockierte laden
            var blockedResult = await _mediator.Request(new GetUserBlockedHttpRequest());

            _blockedIds.Clear();
            if (blockedResult.Result?.Properties != null)
            {
                foreach (var prop in blockedResult.Result.Properties)
                {
                    _blockedIds.Add(prop.Id);
                }
            }

            _isLoaded = true;
            _logger.LogInformation("[PropertyStatus] Loaded {FavoriteCount} favorites, {BlockedCount} blocked",
                _favoriteIds.Count, _blockedIds.Count);

            // StatusChanged wird hier bewusst NICHT gefeuert um rekursive Binding-Kaskaden zu vermeiden.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyStatus] Error loading user status");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public void ClearCache()
    {
        _favoriteIds.Clear();
        _blockedIds.Clear();
        _isLoaded = false;
        // StatusChanged wird hier bewusst NICHT gefeuert um rekursive Binding-Kaskaden zu vermeiden.
    }

    public async Task EnsureLoadedAsync()
    {
        if (!_isLoaded && _authService.IsAuthenticated)
        {
            await RefreshStatusAsync();
        }
    }
}
