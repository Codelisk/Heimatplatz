using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service to manage user's property favorites and blocked status.
/// Caches the status locally and syncs with API.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
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

        // Clear cache when user logs out
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
            // Reload status when user logs in
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
                // Remove from favorites
                var result = await _mediator.Request(
                    new Heimatplatz.Core.ApiClient.Generated.RemoveFavoriteHttpRequest
                    {
                        PropertyId = propertyId
                    });

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
                // Add to favorites
                var result = await _mediator.Request(
                    new Heimatplatz.Core.ApiClient.Generated.AddFavoriteHttpRequest
                    {
                        Body = new Heimatplatz.Core.ApiClient.Generated.AddFavoriteRequest
                        {
                            PropertyId = propertyId
                        }
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

        return isFavorite; // Return original state on error
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
                // Remove from blocked
                var result = await _mediator.Request(
                    new Heimatplatz.Core.ApiClient.Generated.RemoveBlockedHttpRequest
                    {
                        PropertyId = propertyId
                    });

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
                // Add to blocked
                var result = await _mediator.Request(
                    new Heimatplatz.Core.ApiClient.Generated.AddBlockedHttpRequest
                    {
                        Body = new Heimatplatz.Core.ApiClient.Generated.AddBlockedRequest
                        {
                            PropertyId = propertyId
                        }
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

        return isBlocked; // Return original state on error
    }

    public async Task RefreshStatusAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            ClearCache();
            return;
        }

        // Reentrancy guard: prevent recursive calls from StatusChanged event handlers
        // that trigger EnsureLoadedAsync → RefreshStatusAsync again
        if (_isRefreshing)
        {
            _logger.LogInformation("[PropertyStatus] RefreshStatusAsync skipped - already refreshing");
            return;
        }

        _isRefreshing = true;
        try
        {
            _logger.LogInformation("[PropertyStatus] Loading user favorites and blocked...");

            // Load favorites
            var favoritesResult = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserFavoritesHttpRequest());

            _favoriteIds.Clear();
            if (favoritesResult.Result?.Properties != null)
            {
                foreach (var prop in favoritesResult.Result.Properties)
                {
                    _favoriteIds.Add(prop.Id);
                }
            }

            // Load blocked
            var blockedResult = await _mediator.Request(
                new Heimatplatz.Core.ApiClient.Generated.GetUserBlockedHttpRequest());

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

            // StatusChanged intentionally NOT fired here to avoid recursive binding cascade.
            // PropertyCards query status on-demand via RefreshStatusFromService() on flyout opening
            // and via OnPropertyCardLoaded in the page code-behind.
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
        // StatusChanged intentionally NOT fired here to avoid recursive binding cascade.
    }

    public async Task EnsureLoadedAsync()
    {
        if (!_isLoaded && _authService.IsAuthenticated)
        {
            await RefreshStatusAsync();
        }
    }
}
