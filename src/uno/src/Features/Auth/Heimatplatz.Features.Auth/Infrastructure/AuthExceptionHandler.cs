using System.Net;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Infrastructure;

/// <summary>
/// Globaler Exception-Handler fuer Authentifizierungsfehler.
/// Faengt 401 Unauthorized Fehler ab und bereinigt den Auth-State.
/// Registriert in ServiceCollectionExtensions.AddAuthFeature()
/// </summary>
public class AuthExceptionHandler(
    IAuthService authService,
    ILogger<AuthExceptionHandler> logger
) : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        // Pruefen ob es sich um einen 401 Unauthorized Fehler handelt
        if (!IsUnauthorizedError(exception))
        {
            return Task.FromResult(false);
        }

        logger.LogWarning(
            "[AuthExceptionHandler] 401 after failed refresh attempt for {MessageType} - clearing auth state",
            context.Message.GetType().Name);

        // Auth-State bereinigen
        authService.ClearAuthentication();

        // Exception wurde behandelt - andere Handler muessen nicht mehr aufgerufen werden
        // Die Exception wird trotzdem weiter geworfen, damit der aufrufende Code sie behandeln kann
        return Task.FromResult(false);
    }

    /// <summary>
    /// Prueft ob die Exception ein 401 Unauthorized Fehler ist
    /// </summary>
    private static bool IsUnauthorizedError(Exception exception)
    {
        // HttpRequestException mit StatusCode pruefen
        if (exception is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == HttpStatusCode.Unauthorized)
            {
                return true;
            }
        }

        // Message auf 401 pruefen (fuer verschachtelte Exceptions)
        if (exception.Message.Contains("401") ||
            exception.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // InnerException rekursiv pruefen
        if (exception.InnerException != null)
        {
            return IsUnauthorizedError(exception.InnerException);
        }

        return false;
    }
}
