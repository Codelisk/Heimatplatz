using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Presentation;
using Heimatplatz.Maui.Features.Properties.Presentation.Wizard;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Extensions.Stores;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Core.Screenshots;

/// <summary>
/// Deterministischer Screenshot-Modus fuer Store-Aufnahmen (Cake-Tasks "IosScreenshots"
/// und "AndroidScreenshots"). Aktivierung ausschliesslich ueber Prozess-Umgebungsvariablen:
/// auf iOS reicht "xcrun simctl launch" sie mit dem Prefix SIMCTL_CHILD_ durch, auf Android
/// uebersetzt ScreenshotSysProps (nur im Emulator) "debug.heimatplatz.*"-Sysprops in Env-Vars.
/// Auf echten Geraeten und im Store-Build sind diese Variablen nie gesetzt, der Modus bleibt inaktiv.
/// Status geht via Console.WriteLine ins os_log (Cake liest es mit "log show" aus);
/// Shell.Loaded feuert auf iOS nicht zuverlaessig, daher Delay + Dispatcher statt Event.
/// </summary>
public static class ScreenshotMode
{
    public static bool IsActive =>
        Environment.GetEnvironmentVariable("SCREENSHOT_MODE") == "1";

    /// <summary>
    /// Shell-Route, die nach dem Start angesteuert wird (leer = Startseite).
    /// Neben echten Shell-Routen ("///Favorites") gibt es Pseudo-Routen fuer
    /// parametrisierte Seiten ([ShellProperty]-Werte setzen nur die generierten
    /// Nav-Methoden, GoToAsync mit Query-String greift nicht):
    ///   "detail" = Detailseite des neuesten Haus-Inserats,
    ///   "edit"   = WYSIWYG-Editor fuer das erste eigene Inserat.
    /// </summary>
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
                switch (Route)
                {
                    case "detail":
                        await NavigateDetailAsync(services);
                        break;
                    case "edit":
                        await NavigateEditAsync(services);
                        break;
                    default:
                        await shell.Dispatcher.DispatchAsync(() => shell.GoToAsync(Route));
                        break;
                }
                Log($"navigated to '{Route}'");
            }
        }
        catch (Exception ex)
        {
            // Screenshot faellt dann sichtbar falsch aus - Fehler nur loggen, nicht crashen
            Log($"FAILED: {ex}");
        }
    }

    /// <summary>Detailseite des neuesten Haus-Inserats (Seed: kuratierte Fotos).</summary>
    private static async Task NavigateDetailAsync(IServiceProvider services)
    {
        var mediator = services.GetRequiredService<IMediator>();
        var (_, result) = await mediator.Request(new GetPropertiesHttpRequest { Page = 0, PageSize = 20 });

        var property = result?.Properties?.FirstOrDefault(p => p.Type == PropertyType.House)
            ?? result?.Properties?.FirstOrDefault();
        if (property == null)
        {
            Log("detail: no properties returned - staying on home");
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
            services.GetRequiredService<INavigator>()
                .NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = property.Id.ToString()));
    }

    /// <summary>WYSIWYG-Editor (Edit-Modus) fuer das erste eigene Inserat des Screenshot-Users.</summary>
    private static async Task NavigateEditAsync(IServiceProvider services)
    {
        var mediator = services.GetRequiredService<IMediator>();
        var (_, result) = await mediator.Request(new GetUserPropertiesHttpRequest { Page = 0, PageSize = 5 });

        var property = result?.Properties?.FirstOrDefault();
        if (property == null)
        {
            Log("edit: user has no properties - staying on home");
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
            services.GetRequiredService<INavigator>()
                .NavigateTo<PropertyWizardViewModel>(vm => vm.EditPropertyId = property.Id.ToString()));
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
                Password = password
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
