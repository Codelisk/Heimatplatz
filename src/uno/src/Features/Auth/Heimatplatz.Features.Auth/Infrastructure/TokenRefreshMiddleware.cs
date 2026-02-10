using System.Net;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Infrastructure;

/// <summary>
/// Globale Middleware die bei 401 Unauthorized automatisch einen Token-Refresh versucht.
/// Wrapt jeden Mediator-Request und wiederholt ihn nach erfolgreichem Refresh.
/// </summary>
[MiddlewareOrder(-50)]
public class TokenRefreshMiddleware<TRequest, TResult>(
    IAuthService authService,
    ILogger<TokenRefreshMiddleware<TRequest, TResult>> logger
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    private static readonly SemaphoreSlim RefreshSemaphore = new(1, 1);

    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Nicht authentifiziert → skip
        if (!authService.IsAuthenticated)
            return await next().ConfigureAwait(false);

        // Refresh-Request selbst → skip (Endlosschleifen-Schutz)
        if (typeof(TRequest) == typeof(RefreshTokenHttpRequest))
            return await next().ConfigureAwait(false);

        var tokenBeforeRequest = authService.AccessToken;

        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogInformation(
                "[TokenRefresh] 401 fuer {RequestType} - versuche Token-Refresh",
                typeof(TRequest).Name);

            var refreshed = await TryRefreshTokenAsync(context, tokenBeforeRequest, cancellationToken)
                .ConfigureAwait(false);

            if (refreshed)
            {
                logger.LogInformation("[TokenRefresh] Token erfolgreich erneuert - wiederhole Request");
                return await next().ConfigureAwait(false);
            }

            logger.LogWarning("[TokenRefresh] Token-Refresh fehlgeschlagen - 401 wird propagiert");
            throw;
        }
    }

    private async Task<bool> TryRefreshTokenAsync(
        IMediatorContext context,
        string? tokenThatFailed,
        CancellationToken cancellationToken)
    {
        var acquired = await RefreshSemaphore.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
            .ConfigureAwait(false);

        if (!acquired)
        {
            logger.LogWarning("[TokenRefresh] Semaphore-Timeout - Refresh abgebrochen");
            return false;
        }

        try
        {
            // Double-Check: Token wurde bereits von einem anderen Thread erneuert
            if (authService.AccessToken != tokenThatFailed)
            {
                logger.LogInformation("[TokenRefresh] Token bereits von anderem Thread erneuert");
                return true;
            }

            var refreshToken = authService.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("[TokenRefresh] Kein Refresh Token vorhanden");
                return false;
            }

            var response = await context.Request(
                new RefreshTokenHttpRequest
                {
                    Body = new RefreshTokenRequest { RefreshToken = refreshToken }
                },
                cancellationToken,
                child => child.BypassMiddlewareEnabled = true
            ).ConfigureAwait(false);

            if (response == null)
            {
                logger.LogWarning("[TokenRefresh] Refresh-Response war null");
                return false;
            }

            authService.UpdateTokens(
                response.AccessToken,
                response.RefreshToken,
                response.ExpiresAt);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[TokenRefresh] Refresh-Aufruf fehlgeschlagen");
            return false;
        }
        finally
        {
            RefreshSemaphore.Release();
        }
    }
}
