using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Core.Screenshots;

/// <summary>
/// Deterministischer Screenshot-Modus fuer App-Store-Aufnahmen (Cake-Task "IosScreenshots").
/// Aktivierung ausschliesslich ueber Prozess-Umgebungsvariablen, die "xcrun simctl launch"
/// mit dem Prefix SIMCTL_CHILD_ an den Simulator-Prozess durchreicht - auf echten Geraeten
/// und im App Store Build sind diese Variablen nie gesetzt, der Modus bleibt inaktiv.
/// </summary>
public static class ScreenshotMode
{
    public static bool IsActive =>
        Environment.GetEnvironmentVariable("SCREENSHOT_MODE") == "1";

    /// <summary>Shell-Route, die nach dem Start angesteuert wird (leer = Startseite).</summary>
    public static string? Route =>
        Environment.GetEnvironmentVariable("SCREENSHOT_ROUTE");

    /// <summary>Credentials fuer den Auto-Login (Seed-Test-User der Test-API).</summary>
    private static string? LoginEmail =>
        Environment.GetEnvironmentVariable("SCREENSHOT_LOGIN_EMAIL");

    private static string? LoginPassword =>
        Environment.GetEnvironmentVariable("SCREENSHOT_LOGIN_PASSWORD");

    /// <summary>
    /// Wartezeit in Millisekunden nach Shell.Loaded, bevor navigiert wird -
    /// gibt der Startseite Zeit, ihre Daten zu laden (SCREENSHOT_NAV_DELAY_MS).
    /// </summary>
    public static int NavigationDelayMs =>
        int.TryParse(Environment.GetEnvironmentVariable("SCREENSHOT_NAV_DELAY_MS"), out var ms) ? ms : 1500;

    /// <summary>
    /// Meldet nach dem Laden der Shell optional den Test-User an und navigiert
    /// deterministisch zur konfigurierten Route.
    /// Muss vor dem Anzeigen der Shell aufgerufen werden (App.CreateWindow).
    /// </summary>
    public static void TryApply(Shell shell, IServiceProvider services)
    {
        if (!IsActive)
            return;

        shell.Loaded += async (_, _) =>
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ScreenshotMode");
            try
            {
                await LoginAsync(services, logger);
                await Task.Delay(NavigationDelayMs);

                if (!string.IsNullOrWhiteSpace(Route))
                    await shell.GoToAsync(Route);
            }
            catch (Exception ex)
            {
                // Screenshot faellt dann sichtbar falsch aus - Fehler nur loggen, nicht crashen
                logger.LogError(ex, "Screenshot-Modus: Login/Navigation fehlgeschlagen");
            }
        };
    }

    private static async Task LoginAsync(IServiceProvider services, ILogger logger)
    {
        var email = LoginEmail;
        var password = LoginPassword;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var mediator = services.GetRequiredService<IMediator>();
        var (_, result) = await mediator.Request(new LoginHttpRequest
        {
            Body = new LoginRequest
            {
                Email = email,
                Passwort = password
            }
        });

        if (result == null)
        {
            logger.LogWarning("Screenshot-Modus: Login fuer {Email} lieferte kein Ergebnis", email);
            return;
        }

        services.GetRequiredService<IAuthService>().SetAuthenticatedUser(
            result.AccessToken,
            result.RefreshToken,
            result.UserId,
            result.Email,
            result.FullName,
            result.ExpiresAt);

        // Bewusst KEIN UserLoggedInEvent und kein Push-Init: der iOS-Permission-Dialog
        // wuerde ueber dem Screenshot liegen
        logger.LogInformation("Screenshot-Modus: angemeldet als {Email}", email);
    }
}
