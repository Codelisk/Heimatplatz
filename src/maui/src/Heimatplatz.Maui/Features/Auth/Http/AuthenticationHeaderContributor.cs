using Heimatplatz.Maui.Http;
using Shiny;

namespace Heimatplatz.Maui.Features.Auth.Http;

/// <summary>
/// Adds JWT Bearer authentication token to HTTP requests.
/// </summary>
[Singleton]
public class AuthenticationHeaderContributor(IAuthService authService) : IHttpHeaderContributor
{
    /// <summary>
    /// Authentication headers should be added first (priority 0-10).
    /// </summary>
    public int Priority => 0;

    public Task ContributeAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.AccessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                authService.AccessToken);
        }

        return Task.CompletedTask;
    }
}
