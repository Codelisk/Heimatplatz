using System.Text.Json;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Shiny.Extensions.DependencyInjection;
using Windows.Storage;

namespace Heimatplatz.Features.Properties.Services;

/// <summary>
/// Service fuer das Laden und Speichern von Benutzer-Filtereinstellungen.
/// Verwendet lokalen Speicher (ApplicationData.LocalSettings) als Fallback,
/// bis die API-Endpoints verfuegbar sind.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class FilterPreferencesService : IFilterPreferencesService
{
    private const string StorageKey = "FilterPreferences";
    private readonly IAuthService _authService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public FilterPreferencesService(IAuthService authService)
    {
        _authService = authService;
    }

    /// <inheritdoc />
    public Task<FilterPreferencesDto?> GetPreferencesAsync(CancellationToken ct = default)
    {
        if (!_authService.IsAuthenticated || _authService.UserId == null)
        {
            return Task.FromResult<FilterPreferencesDto?>(null);
        }

        var key = GetUserStorageKey();
        var settings = ApplicationData.Current.LocalSettings;

        if (settings.Values.TryGetValue(key, out var value) && value is string json)
        {
            try
            {
                var preferences = JsonSerializer.Deserialize<FilterPreferencesDto>(json, JsonOptions);
                return Task.FromResult(preferences);
            }
            catch (JsonException)
            {
                // Corrupted data - return null
                return Task.FromResult<FilterPreferencesDto?>(null);
            }
        }

        return Task.FromResult<FilterPreferencesDto?>(null);
    }

    /// <inheritdoc />
    public Task SavePreferencesAsync(FilterPreferencesDto preferences, CancellationToken ct = default)
    {
        if (!_authService.IsAuthenticated || _authService.UserId == null)
        {
            return Task.CompletedTask;
        }

        var key = GetUserStorageKey();
        var json = JsonSerializer.Serialize(preferences, JsonOptions);
        var settings = ApplicationData.Current.LocalSettings;

        settings.Values[key] = json;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearPreferencesAsync(CancellationToken ct = default)
    {
        if (!_authService.IsAuthenticated || _authService.UserId == null)
        {
            return Task.CompletedTask;
        }

        var key = GetUserStorageKey();
        var settings = ApplicationData.Current.LocalSettings;

        settings.Values.Remove(key);

        return Task.CompletedTask;
    }

    private string GetUserStorageKey()
    {
        return $"{StorageKey}_{_authService.UserId}";
    }
}
