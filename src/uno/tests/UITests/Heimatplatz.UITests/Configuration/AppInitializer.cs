using Uno.UITest;
using Uno.UITest.Selenium;

namespace Heimatplatz.UITests.Configuration;

/// <summary>
/// Initialisiert die App fuer verschiedene Plattformen.
/// Basiert auf Uno.UITest: https://github.com/unoplatform/Uno.UITest
///
/// WICHTIG: Vor dem Ausfuehren der Tests muss die App gestartet und die
/// WebAssemblyDefaultUri in Constants.cs angepasst werden.
/// </summary>
public static class AppInitializer
{
    private static IApp? _app;
    private static bool _initialized;

    /// <summary>
    /// Gibt die aktuelle App-Instanz zurueck oder erstellt eine neue.
    /// </summary>
    public static IApp App => _app ?? throw new InvalidOperationException(
        "App wurde nicht initialisiert. Rufe zuerst StartApp() auf.");

    /// <summary>
    /// Initialisiert die Test-Umgebung einmalig (Cold Start).
    /// Sollte in einem statischen Konstruktor oder [OneTimeSetUp] aufgerufen werden.
    /// </summary>
    public static void ColdStartApp()
    {
        if (_initialized)
        {
            return;
        }

        _app = CreateApp();
        _initialized = true;
    }

    /// <summary>
    /// Erstellt die App-Instanz basierend auf der Plattform.
    /// </summary>
    private static IApp CreateApp()
    {
        return Constants.CurrentPlatform switch
        {
            Platform.Browser => CreateBrowserApp(),
            Platform.Android => CreateAndroidApp(),
            Platform.iOS => CreateiOSApp(),
            _ => CreateBrowserApp()
        };
    }

    /// <summary>
    /// Erstellt eine Browser/WebAssembly App-Instanz.
    /// </summary>
    private static IApp CreateBrowserApp()
    {
        return ConfigureApp
            .WebAssembly
            .Uri(new Uri(Constants.WebAssemblyDefaultUri))
            .StartApp();
    }

    /// <summary>
    /// Erstellt eine Android App-Instanz.
    /// </summary>
    private static IApp CreateAndroidApp()
    {
        // Android Tests erfordern Xamarin.UITest auf dem Build-Agent
        // Fuer reine WebAssembly-Projekte wird Browser als Fallback verwendet
        return CreateBrowserApp();
    }

    /// <summary>
    /// Erstellt eine iOS App-Instanz.
    /// </summary>
    private static IApp CreateiOSApp()
    {
        // iOS Tests erfordern Xamarin.UITest auf macOS
        // Fuer reine WebAssembly-Projekte wird Browser als Fallback verwendet
        return CreateBrowserApp();
    }

    /// <summary>
    /// Verbindet sich mit der laufenden App (fuer jeden Test).
    /// </summary>
    public static IApp AttachToApp()
    {
        if (!_initialized)
        {
            ColdStartApp();
        }

        return _app!;
    }

    /// <summary>
    /// Startet die App fuer die konfigurierte Plattform.
    /// Wrapper fuer AttachToApp() mit Rueckwaertskompatibilitaet.
    /// </summary>
    public static IApp StartApp()
    {
        return AttachToApp();
    }

    /// <summary>
    /// Beendet die aktuelle App-Session.
    /// </summary>
    public static void StopApp()
    {
        // App-Session bleibt offen fuer weitere Tests
        // Nur Reset wenn explizit gewuenscht
    }

    /// <summary>
    /// Beendet die App-Session komplett und gibt Ressourcen frei.
    /// </summary>
    public static void TearDown()
    {
        _app = null;
        _initialized = false;
    }

    /// <summary>
    /// Gibt die aktuelle Plattform zurueck.
    /// </summary>
    public static Platform GetLocalPlatform() => Constants.CurrentPlatform;
}
