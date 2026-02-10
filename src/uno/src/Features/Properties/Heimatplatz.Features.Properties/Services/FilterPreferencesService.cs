using System.Net;
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
    private const string RefreshEndpoint = "/api/auth/refresh";
    private static readonly SemaphoreSlim RefreshSemaphore = new(1, 1);

    /// <summary>
    /// Sets the Authorization header with the current access token.
    /// </summary>
    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(authService.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authService.AccessToken);
        }
    }

    /// <summary>
    /// Versucht den Token zu erneuern wenn ein Refresh-Token vorhanden ist.
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        var refreshToken = authService.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var acquired = await RefreshSemaphore.WaitAsync(TimeSpan.FromSeconds(10), ct);
        if (!acquired)
            return false;

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                RefreshEndpoint,
                new { RefreshToken = refreshToken },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[FilterPreferences] Token-Refresh fehlgeschlagen: {StatusCode}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenRefreshResult>(ct);
            if (result == null)
                return false;

            authService.UpdateTokens(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            logger.LogInformation("[FilterPreferences] Token erfolgreich erneuert");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[FilterPreferences] Token-Refresh fehlgeschlagen");
            return false;
        }
        finally
        {
            RefreshSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        if (authService.UserId == null || string.IsNullOrEmpty(authService.RefreshToken))
        {
            return null;
        }

        try
        {
            SetAuthorizationHeader();

            var response = await httpClient.GetAsync(ApiEndpoint, ct);

            // Bei 401: Token refreshen und nochmal versuchen
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogInformation("[FilterPreferences] 401 erhalten - versuche Token-Refresh");
                if (await TryRefreshTokenAsync(ct))
                {
                    SetAuthorizationHeader();
                    response = await httpClient.GetAsync(ApiEndpoint, ct);
                }
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetUserFilterPreferencesResponse>(ct);
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

            // Bei 401: Token refreshen und nochmal versuchen
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogInformation("[FilterPreferences] 401 beim Speichern - versuche Token-Refresh");
                if (await TryRefreshTokenAsync(ct))
                {
                    SetAuthorizationHeader();
                    response = await httpClient.PostAsJsonAsync(ApiEndpoint, request, ct);
                }
            }

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

internal record TokenRefreshResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
