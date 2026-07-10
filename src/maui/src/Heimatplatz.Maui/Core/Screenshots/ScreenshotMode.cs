using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Extensions.Stores;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Core.Screenshots;

/// <summary>
/// Deterministischer Screenshot-Modus fuer App-Store-Aufnahmen (Cake-Task "IosScreenshots").
/// Aktivierung ausschliesslich ueber Prozess-Umgebungsvariablen, die "xcrun simctl launch"
/// mit dem Prefix SIMCTL_CHILD_ an den Simulator-Prozess durchreicht - auf echten Geraeten
/// und im App Store Build sind diese Variablen nie gesetzt, der Modus bleibt inaktiv.
/// Status geht via Console.WriteLine ins os_log (Cake liest es mit "log show" aus);
/// Shell.Loaded feuert auf iOS nicht zuverlaessig, daher Delay + Dispatcher statt Event.
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
    /// Wartezeit in Millisekunden nach dem Start, bevor Login/Navigation laufen -
    /// gibt der Shell Zeit zu laden (SCREENSHOT_NAV_DELAY_MS).
    /// </summary>
    public static int NavigationDelayMs =>
        int.TryParse(Environment.GetEnvironmentVariable("SCREENSHOT_NAV_DELAY_MS"), out var ms) ? ms : 1500;

    /// <summary>
    /// Ersetzt im Screenshot-Modus den Secure Store (Keychain) durch den Default-Store:
    /// die Ad-hoc-Simulator-Signatur darf keine Keychain-Entitlements tragen (launchd
    /// verweigert sonst den Spawn), Keychain-Zugriffe schlagen mit MissingEntitlement
    /// fehl. Muss vor dem ersten Store-Zugriff laufen (Anfang von CreateMauiApp).
    /// </summary>
    public static void TryOverrideSecureStore()
    {
        if (!IsActive)
            return;

        Stores.Register(StoreKeys.Secure, Stores.Default);
        Log("secure store overridden with default store");
    }

    /// <summary>
    /// Meldet nach dem Start optional den Test-User an und navigiert deterministisch
    /// zur konfigurierten Route. Muss vor dem Anzeigen der Shell aufgerufen werden
    /// (App.CreateWindow).
    /// </summary>
    public static void TryApply(Shell shell, IServiceProvider services)
    {
        if (!IsActive)
            return;

        Log($"active, route='{Route}', login={!string.IsNullOrEmpty(LoginEmail)}");
        _ = ApplyAsync(shell, services);
    }

    private static async Task ApplyAsync(Shell shell, IServiceProvider services)
    {
        try
        {
            await Task.Delay(NavigationDelayMs);

            // Login-Fehler nicht die Navigation blocken lassen - der Screenshot zeigt
            // dann den nicht angemeldeten Zustand, was im Log klar erkennbar ist
            try
            {
                await LoginAsync(services);
            }
            catch (Exception ex)
            {
                Log($"login FAILED: {ex}");
            }

            if (!string.IsNullOrWhiteSpace(Route))
            {
                await shell.Dispatcher.DispatchAsync(() => shell.GoToAsync(Route));
                Log($"navigated to '{Route}'");
            }
        }
        catch (Exception ex)
        {
            // Screenshot faellt dann sichtbar falsch aus - Fehler nur loggen, nicht crashen
            Log($"FAILED: {ex}");
        }
    }

    private static async Task LoginAsync(IServiceProvider services)
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
            Log($"login for {email} returned no result");
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
        Log($"logged in as {email}");
    }

    /// <summary>Console.WriteLine landet auf iOS im os_log und ist via "log show" auslesbar.</summary>
    private static void Log(string message) =>
        Console.WriteLine($"[ScreenshotMode] {message}");
}
