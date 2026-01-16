using Heimatplatz;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Http;
using Shiny.Extensions.DependencyInjection;

namespace Heimatplatz.Features.Auth.Http;

/// <summary>
/// Adds JWT Bearer authentication token to HTTP requests.
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class AuthenticationHeaderContributor : IHttpHeaderContributor
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Authentication headers should be added first (priority 0-10).
    /// </summary>
    public int Priority => 0;

    public AuthenticationHeaderContributor(IAuthService authService)
    {
        _authService = authService;
    }

    public Task ContributeAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        // Add Authorization header if user is authenticated
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                _authService.AccessToken);
        }

        return Task.CompletedTask;
    }
}
