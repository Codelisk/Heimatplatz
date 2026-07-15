using System.Net;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Auth.Infrastructure;

/// <summary>
/// Globaler Exception-Handler fuer Authentifizierungsfehler.
/// Faengt 401 Unauthorized Fehler ab und bereinigt den Auth-State.
/// </summary>
public class AuthExceptionHandler(
    IAuthService authService,
    ILogger<AuthExceptionHandler> logger
) : IExceptionHandler
{
    public async Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        if (!IsUnauthorizedError(exception))
            return false;

        logger.LogWarning(
            "[AuthExceptionHandler] 401 after failed refresh attempt for {MessageType} - clearing auth state",
            context.Message.GetType().Name);

        var userCachePrefix = UserScopedContractKeyProvider.GetScopePrefix(authService.UserId);
        await context.FlushStores(userCachePrefix, partialMatch: true);
        authService.ClearAuthentication();

        // Die Exception wird trotzdem weiter geworfen, damit der aufrufende Code sie behandeln kann
        return false;
    }

    private static bool IsUnauthorizedError(Exception exception)
    {
        if (exception is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.Unauthorized)
            return true;

        if (exception.Message.Contains("401") ||
            exception.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            return true;

        if (exception.InnerException != null)
            return IsUnauthorizedError(exception.InnerException);

        return false;
    }
}
