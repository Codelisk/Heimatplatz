#if DEBUG
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Debug.Services;

/// <summary>
/// Debug-Service fuer schnellen Login mit Test-Usern
/// Nur in DEBUG-Builds verfuegbar
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class DebugAuthService : IDebugAuthService
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly ILogger<DebugAuthService> _logger;

    public DebugAuthService(
        IMediator mediator,
        IAuthService authService,
        ILogger<DebugAuthService> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> QuickLoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("[DEBUG] Quick Login fuer {Email}", email);

            var (_, result) = await _mediator.Request(new LoginHttpRequest
            {
                Body = new LoginRequest
                {
                    Email = email,
                    Passwort = password
                }
            });

            if (result == null)
            {
                _logger.LogWarning("[DEBUG] Login fehlgeschlagen fuer {Email}", email);
                return false;
            }

            _authService.SetAuthenticatedUser(
                result.AccessToken,
                result.RefreshToken,
                result.UserId,
                result.Email,
                result.FullName,
                result.ExpiresAt);

            _logger.LogInformation("[DEBUG] Login erfolgreich fuer {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG] Quick Login fehlgeschlagen fuer {Email}", email);
            return false;
        }
    }

    /// <inheritdoc />
    public Task LogoutAsync()
    {
        _authService.ClearAuthentication();
        _logger.LogInformation("[DEBUG] Logout durchgefuehrt");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string? GetCurrentToken()
    {
        return _authService.AccessToken;
    }

    /// <inheritdoc />
    public string? GetCurrentUserEmail()
    {
        return _authService.UserEmail;
    }

    /// <inheritdoc />
    public string GetCurrentUserRoles()
    {
        var token = _authService.AccessToken;
        if (string.IsNullOrEmpty(token))
            return "Nicht angemeldet";

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var roles = jwtToken.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();

            return roles.Any() ? string.Join(", ", roles) : "Keine Rollen";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEBUG] Fehler beim Parsen des JWT-Tokens");
            return "Fehler beim Lesen der Rollen";
        }
    }
}
#endif
