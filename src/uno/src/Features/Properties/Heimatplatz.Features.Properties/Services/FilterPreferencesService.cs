using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer das Laden und Speichern von Benutzer-Filtereinstellungen.
/// Verwendet die API-Endpoints zum Speichern in der Datenbank.
/// Registriert via AddHttpClient in ServiceCollectionExtensions.
/// </summary>
public class FilterPreferencesService(
    HttpClient httpClient,
    IAuthService authService,
    ILogger<FilterPreferencesService> logger
) : IFilterPreferencesService
{
    private const string ApiEndpoint = "/api/auth/filter-preferences";

    /// <summary>
    /// Sets the Authorization header with the current access token.
    /// </summary>
    private void SetAuthorizationHeader()
    {
        if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authService.AccessToken);
        }
    }

    /// <inheritdoc />
    public async Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        if (!authService.IsAuthenticated || authService.UserId == null)
        {
            return null;
        }

        try
        {
            SetAuthorizationHeader();

            var response = await httpClient.GetFromJsonAsync<GetUserFilterPreferencesResponse>(
                ApiEndpoint,
                ct);

            if (response == null)
            {
                logger.LogWarning("Failed to get filter preferences - null response");
                return FilterPreferencesDto.Default;
            }

            return new FilterPreferencesDto(
                SelectedOrte: response.SelectedOrtes,
                SelectedAgeFilter: (AgeFilter)response.SelectedAgeFilter,
                IsHausSelected: response.IsHausSelected,
                IsGrundstueckSelected: response.IsGrundstueckSelected,
                IsZwangsversteigerungSelected: response.IsZwangsversteigerungSelected,
                IsPrivateSelected: response.IsPrivateSelected,
                IsBrokerSelected: response.IsBrokerSelected,
                IsPortalSelected: response.IsPortalSelected,
                ExcludedSellerSourceIds: response.ExcludedSellerSourceIds
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

        if (!authService.IsAuthenticated || authService.UserId == null)
        {
            logger.LogWarning("User not authenticated - skipping save");
            return;
        }

        try
        {
            SetAuthorizationHeader();

            var request = new SaveUserFilterPreferencesRequest(
                SelectedOrtes: preferences.SelectedOrte,
                SelectedAgeFilter: (int)preferences.SelectedAgeFilter,
                IsHausSelected: preferences.IsHausSelected,
                IsGrundstueckSelected: preferences.IsGrundstueckSelected,
                IsZwangsversteigerungSelected: preferences.IsZwangsversteigerungSelected,
                IsPrivateSelected: preferences.IsPrivateSelected,
                IsBrokerSelected: preferences.IsBrokerSelected,
                IsPortalSelected: preferences.IsPortalSelected,
                ExcludedSellerSourceIds: preferences.ExcludedSellerSourceIds
            );

            var response = await httpClient.PostAsJsonAsync(ApiEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to save filter preferences - Status: {StatusCode}", response.StatusCode);
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

// DTOs matching API contracts
internal record GetUserFilterPreferencesResponse(
    List<string> SelectedOrtes,
    int SelectedAgeFilter,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid> ExcludedSellerSourceIds
);

internal record SaveUserFilterPreferencesRequest(
    List<string> SelectedOrtes,
    int SelectedAgeFilter,
    bool IsHausSelected,
    bool IsGrundstueckSelected,
    bool IsZwangsversteigerungSelected,
    bool IsPrivateSelected,
    bool IsBrokerSelected,
    bool IsPortalSelected,
    List<Guid> ExcludedSellerSourceIds
);
