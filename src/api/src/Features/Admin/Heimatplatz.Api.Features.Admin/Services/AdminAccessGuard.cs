using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Features.Admin.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.Admin.Services;

/// <summary>
/// Shared-Key-Pruefung fuer die Admin-Endpoints - gleiches Muster wie der Edikte-Sync-
/// Trigger (TriggerForeclosureAuctionSyncHandler): kein JWT/RequireAdmin, weil es auf
/// Prod keinen echten Admin-Account gibt. Der Astro-SSR-Server schickt ADMIN_API_KEY
/// als X-Admin-Key-Header mit; ohne konfigurierten Key ist der Zugang ausserhalb von
/// Development gesperrt (fail-closed). Defense-in-depth zusaetzlich zur Caddy-IP-Sperre
/// auf /api/admin* (deploy/hetzner/Caddyfile).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class AdminAccessGuard(
    IHttpContextAccessor httpContextAccessor,
    IOptions<AdminOptions> options,
    IHostEnvironment environment
) : IAdminAccessGuard
{
    public const string ApiKeyHeader = "X-Admin-Key";

    public void EnsureAuthorized()
    {
        var providedKey = httpContextAccessor.HttpContext?.Request.Headers[ApiKeyHeader].ToString();

        if (!SharedKeyAuthorization.IsAuthorized(options.Value.ApiKey, providedKey, environment.IsDevelopment()))
            throw new UnauthorizedAccessException("Admin-Key fehlt oder ist ungueltig.");
    }
}
