using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer das Laden und Speichern von Benutzer-Filtereinstellungen.
/// Verwendet Shiny Mediator HTTP Requests (generiert aus OpenAPI).
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class FilterPreferencesService(
    IMediator mediator,
    IAuthService authService,
    ILogger<FilterPreferencesService> logger
) : IFilterPreferencesService
{
    /// <inheritdoc />
    public async Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        if (authService.UserId == null || string.IsNullOrEmpty(authService.RefreshToken))
        {
            return null;
        }

        try
        {
            var (_, result) = await mediator.Request(new GetUserFilterPreferencesHttpRequest(), ct);
            if (result == null)
            {
                logger.LogWarning("Failed to get filter preferences - null response");
                return FilterPreferencesDto.Default;
            }

            return new FilterPreferencesDto(
                SelectedOrte: result.SelectedOrtes,
                SelectedAgeFilter: (AgeFilter)result.SelectedAgeFilter,
                IsHausSelected: result.IsHausSelected,
                IsGrundstueckSelected: result.IsGrundstueckSelected,
                IsZwangsversteigerungSelected: result.IsZwangsversteigerungSelected,
                IsPrivateSelected: result.IsPrivateSelected,
                IsBrokerSelected: result.IsBrokerSelected,
                IsPortalSelected: result.IsPortalSelected,
                ExcludedSellerSourceIds: result.ExcludedSellerSourceIds
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error getting filter preferences");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting filter preferences");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SavePreferencesAsync(FilterPreferencesDto preferences, CancellationToken ct = default)
    {
        logger.LogInformation(
            "SavePreferencesAsync called. IsAuthenticated: {IsAuth}, UserId: {UserId}, AccessToken: {HasToken}",
            authService.IsAuthenticated,
            authService.UserId,
            !string.IsNullOrEmpty(authService.AccessToken));

        if (authService.UserId == null || string.IsNullOrEmpty(authService.RefreshToken))
        {
            logger.LogWarning("User not authenticated - skipping save");
            return;
        }

        try
        {
            var (_, result) = await mediator.Request(new SaveUserFilterPreferencesHttpRequest
            {
                Body = new SaveUserFilterPreferencesRequest
                {
                    SelectedOrtes = preferences.SelectedOrte,
                    SelectedAgeFilter = (int)preferences.SelectedAgeFilter,
                    IsHausSelected = preferences.IsHausSelected,
                    IsGrundstueckSelected = preferences.IsGrundstueckSelected,
                    IsZwangsversteigerungSelected = preferences.IsZwangsversteigerungSelected,
                    IsPrivateSelected = preferences.IsPrivateSelected,
                    IsBrokerSelected = preferences.IsBrokerSelected,
                    IsPortalSelected = preferences.IsPortalSelected,
                    ExcludedSellerSourceIds = preferences.ExcludedSellerSourceIds
                }
            }, ct);

            if (result is not { Success: true })
            {
                logger.LogWarning("Failed to save filter preferences");
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error saving filter preferences");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving filter preferences");
            throw;
        }
    }

    /// <inheritdoc />
    public Task ClearPreferencesAsync(CancellationToken ct = default)
    {
        // Clearing means saving default preferences
        return SavePreferencesAsync(FilterPreferencesDto.Default, ct);
    }
}
