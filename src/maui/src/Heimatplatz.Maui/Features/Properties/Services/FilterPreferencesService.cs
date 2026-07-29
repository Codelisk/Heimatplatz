using System.Text.Json;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Service fuer das Laden und Speichern von Benutzer-Filtereinstellungen.
/// Verwendet Shiny Mediator HTTP Requests (generiert aus OpenAPI).
/// Gaeste speichern lokal (Preferences); der Stand wird beim naechsten Login
/// ans Konto uebernommen (Pending-Mechanismus, gleiches Muster wie das Web).
/// </summary>
[Singleton]
public class FilterPreferencesService : IFilterPreferencesService
{
    // Lokaler Spiegel (Restore ueber App-Starts) + Pending-Slot (noch nicht ans
    // Konto gesynct: Gast-Stand oder fehlgeschlagener/unterbrochener Save)
    private const string LocalKey = "FilterPreferences.Local";
    private const string PendingKey = "FilterPreferences.Pending";

    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly ILogger<FilterPreferencesService> _logger;

    public FilterPreferencesService(
        IMediator mediator,
        IAuthService authService,
        ILogger<FilterPreferencesService> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _logger = logger;

        // Abmelden entspricht dem Web-clearUserStateCache: lokale Kopien des
        // Konto-Stands raeumen, damit der naechste Gast bei den Defaults startet
        _authService.AuthenticationStateChanged += (_, isAuthenticated) =>
        {
            if (!isAuthenticated)
            {
                Preferences.Default.Remove(LocalKey);
                Preferences.Default.Remove(PendingKey);
            }
        };
    }

    private bool IsAuthenticated =>
        _authService.UserId != null && !string.IsNullOrEmpty(_authService.RefreshToken);

    /// <inheritdoc />
    public async Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return ReadStored(LocalKey);
        }

        // Erst einen etwaigen Pending-Stand (Gast-Filter oder unterbrochener Save)
        // ans Konto uebernehmen - schlaegt das fehl, darf der aeltere Server-Stand
        // den lokalen nicht ueberschreiben.
        if (!await TrySyncPendingAsync(ct))
        {
            return ReadStored(LocalKey);
        }

        try
        {
            var (_, result) = await _mediator.Request(new GetUserFilterPreferencesHttpRequest(), ct);
            if (result == null)
            {
                _logger.LogWarning("Failed to get filter preferences - null response");
                return FilterPreferencesDto.Default;
            }

            var preferences = new FilterPreferencesDto(
                SelectedOrte: result.SelectedOrtes,
                SelectedAgeFilter: (AgeFilter)result.SelectedAgeFilter,
                IsHausSelected: result.IsHausSelected,
                IsGrundstueckSelected: result.IsGrundstueckSelected,
                IsZwangsversteigerungSelected: result.IsZwangsversteigerungSelected,
                IsPrivateSelected: result.IsPrivateSelected,
                IsBrokerSelected: result.IsBrokerSelected,
                ExcludedSellerSourceIds: result.ExcludedSellerSourceIds,
                SelectedSort: (SortOption)result.SelectedSort
            );

            WriteStored(LocalKey, preferences);
            return preferences;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting filter preferences");
            return ReadStored(LocalKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filter preferences");
            return ReadStored(LocalKey);
        }
    }

    /// <inheritdoc />
    public async Task SavePreferencesAsync(FilterPreferencesDto preferences, CancellationToken ct = default)
    {
        // Immer zuerst lokal sichern: Gaeste behalten den Stand ueber App-Starts,
        // und der Pending-Slot ueberlebt App-Kill/Netzfehler vor dem Server-Sync
        var json = WriteStored(LocalKey, preferences);
        Preferences.Default.Set(PendingKey, json);

        if (!IsAuthenticated)
        {
            return;
        }

        await PostPreferencesAsync(preferences, json, ct);
    }

    /// <inheritdoc />
    public Task ClearPreferencesAsync(CancellationToken ct = default)
    {
        // Loeschen bedeutet: Standard-Einstellungen speichern
        return SavePreferencesAsync(FilterPreferencesDto.Default, ct);
    }

    /// <summary>
    /// Uebernimmt einen wartenden Pending-Stand ans Konto. True, wenn nichts
    /// wartet oder der Sync geklappt hat.
    /// </summary>
    private async Task<bool> TrySyncPendingAsync(CancellationToken ct)
    {
        var pendingJson = Preferences.Default.Get<string?>(PendingKey, null);
        if (string.IsNullOrEmpty(pendingJson))
        {
            return true;
        }

        var pending = Deserialize(pendingJson);
        if (pending == null)
        {
            Preferences.Default.Remove(PendingKey);
            return true;
        }

        try
        {
            await PostPreferencesAsync(pending, pendingJson, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pending filter preferences sync failed - keeping local state");
            return false;
        }
    }

    private async Task PostPreferencesAsync(FilterPreferencesDto preferences, string json, CancellationToken ct)
    {
        try
        {
            var (_, result) = await _mediator.Request(new SaveUserFilterPreferencesHttpRequest
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
                    ExcludedSellerSourceIds = preferences.ExcludedSellerSourceIds,
                    SelectedSort = (int)preferences.SelectedSort
                }
            }, ct);

            if (result is not { Success: true })
            {
                _logger.LogWarning("Failed to save filter preferences");
                return;
            }

            // Pending nur raeumen, wenn nicht inzwischen ein neuerer Stand wartet
            if (Preferences.Default.Get<string?>(PendingKey, null) == json)
            {
                Preferences.Default.Remove(PendingKey);
            }
        }
        catch (HttpRequestException ex)
        {
            // Pending bleibt liegen - der naechste Load/Save synct nach
            _logger.LogError(ex, "HTTP error saving filter preferences");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving filter preferences");
            throw;
        }
    }

    private static FilterPreferencesDto? ReadStored(string key)
        => Deserialize(Preferences.Default.Get<string?>(key, null));

    private static string WriteStored(string key, FilterPreferencesDto preferences)
    {
        var json = JsonSerializer.Serialize(preferences);
        Preferences.Default.Set(key, json);
        return json;
    }

    private static FilterPreferencesDto? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FilterPreferencesDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
