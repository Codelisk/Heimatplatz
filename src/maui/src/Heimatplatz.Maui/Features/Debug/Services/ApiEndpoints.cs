using Heimatplatz.Maui.Core.Build;

namespace Heimatplatz.Maui.Features.Debug.Services;

/// <summary>Waehlbare API-Endpunkte des Debug-Umschalters</summary>
public enum ApiEndpointKind
{
    /// <summary>Lokale Entwicklungs-API (plattformabhaengig, siehe GetDevelopmentUrl)</summary>
    Development,

    /// <summary>Test-API am Hetzner-Server (haengt an der Testdatenbank)</summary>
    Test,

    /// <summary>Produktions-API</summary>
    Production
}

/// <summary>
/// Zentrale Konstanten und Aufloesung der API-Endpunkte. Wird beim App-Start
/// (MauiProgram) und vom Debug-Umschalter (ApiEndpointService) verwendet.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>Produktions-API</summary>
    public const string ProductionUrl = "https://api.heimatplatz.at";

    /// <summary>Test-API (gleicher Code wie Prod, Testdatenbank, eigener JWT-Key)</summary>
    public const string TestUrl = "https://test-api.heimatplatz.at";

    /// <summary>Konfigurationsschluessel der Base-URL des generierten Shiny.Mediator OpenAPI-Clients</summary>
    public const string MediatorHttpConfigKey = "Mediator:Http:Heimatplatz.Maui.ApiClient.Generated.*";

    /// <summary>
    /// Preferences-Schluessel der Endpunkt-Auswahl ("Development"|"Test"|"Production").
    /// Nur in Builds mit Entwicklerwerkzeugen beschrieben und gelesen (siehe <see cref="AppChannels"/>).
    /// </summary>
    public const string EndpointPreferenceKey = "debug_api_endpoint";

    /// <summary>
    /// Lokale Entwicklungs-API je Plattform: Der Android-Emulator erreicht den Host ueber
    /// 10.0.2.2, physische Geraete ueber "adb reverse tcp:5292 tcp:5292", Desktop direkt.
    /// </summary>
    public static string GetDevelopmentUrl()
    {
#if ANDROID
        return DeviceInfo.Current.DeviceType == DeviceType.Virtual
            ? "http://10.0.2.2:5292"
            : "http://localhost:5292";
#else
        return "http://localhost:5292";
#endif
    }

    /// <summary>
    /// Plattform-Default solange nie umgeschaltet wurde: Android-Debug lokal,
    /// alle anderen Plattformen Produktion. Interne Test-Builds starten bewusst
    /// ebenfalls auf Produktion - der Tester schaltet bewusst um.
    /// </summary>
    public static ApiEndpointKind GetDefaultEndpoint()
    {
#if DEBUG && ANDROID
        return ApiEndpointKind.Development;
#else
        return ApiEndpointKind.Production;
#endif
    }

    /// <summary>
    /// True, wenn die lokale Entwicklungs-API waehlbar ist. Auf einem TestFlight-
    /// oder Play-Test-Geraet gibt es keinen Host hinter localhost - die Option
    /// waere dort nur eine Falle.
    /// </summary>
    public static bool IsDevelopmentEndpointAvailable =>
        AppChannels.Current == AppChannelKind.Development;

    /// <summary>Base-URL zum gewaehlten Endpunkt</summary>
    public static string GetUrl(ApiEndpointKind kind) => kind switch
    {
        ApiEndpointKind.Development => GetDevelopmentUrl(),
        ApiEndpointKind.Test => TestUrl,
        _ => ProductionUrl
    };

    /// <summary>Kurzname des Endpunkts fuer Diagnoseanzeigen (Flyout-Fusszeile, Debug-Seite)</summary>
    public static string GetDisplayName(ApiEndpointKind kind) => kind switch
    {
        ApiEndpointKind.Development => "Entwicklungs-API",
        ApiEndpointKind.Test => "Test-API",
        _ => "Produktions-API"
    };

    /// <summary>Endpunkt-Art zu einer Base-URL (fuer die Anzeige der effektiv aktiven URL)</summary>
    public static ApiEndpointKind GetKindForUrl(string? url) => url switch
    {
        null or "" => ApiEndpointKind.Production,
        _ when url.Equals(TestUrl, StringComparison.OrdinalIgnoreCase) => ApiEndpointKind.Test,
        _ when url.Equals(ProductionUrl, StringComparison.OrdinalIgnoreCase) => ApiEndpointKind.Production,
        _ => ApiEndpointKind.Development
    };

    /// <summary>
    /// Persistierte Auswahl aus den Preferences (Fallback: Plattform-Default).
    ///
    /// Fail-closed: Ohne Entwicklerwerkzeuge gilt immer Produktion, und eine noch
    /// gespeicherte Auswahl wird verworfen. Das ist der iOS-Pfad, der wirklich
    /// vorkommt: Ein Tester stellt in TestFlight auf Test, bekommt spaeter das
    /// Store-Update ueber dieselbe Installation - ohne dieses Aufraeumen wuerde
    /// die Endkundenversion weiter gegen die Test-API sprechen.
    /// </summary>
    public static ApiEndpointKind GetSelectedEndpoint()
    {
        if (!AppChannels.AreDeveloperToolsEnabled)
        {
            if (Preferences.Default.ContainsKey(EndpointPreferenceKey))
                Preferences.Default.Remove(EndpointPreferenceKey);

            return ApiEndpointKind.Production;
        }

        var value = Preferences.Default.Get(EndpointPreferenceKey, string.Empty);
        return Enum.TryParse<ApiEndpointKind>(value, out var kind) ? kind : GetDefaultEndpoint();
    }
}
