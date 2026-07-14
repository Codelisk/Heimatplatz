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

    /// <summary>Preferences-Schluessel der Debug-Auswahl ("Development"|"Test"|"Production", nur DEBUG-Builds)</summary>
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
    /// alle anderen Plattformen Produktion.
    /// </summary>
    public static ApiEndpointKind GetDefaultEndpoint()
    {
#if DEBUG && ANDROID
        return ApiEndpointKind.Development;
#else
        return ApiEndpointKind.Production;
#endif
    }

    /// <summary>Base-URL zum gewaehlten Endpunkt</summary>
    public static string GetUrl(ApiEndpointKind kind) => kind switch
    {
        ApiEndpointKind.Development => GetDevelopmentUrl(),
        ApiEndpointKind.Test => TestUrl,
        _ => ProductionUrl
    };

    /// <summary>Persistierte Debug-Auswahl aus den Preferences (Fallback: Plattform-Default)</summary>
    public static ApiEndpointKind GetSelectedEndpoint()
    {
        var value = Preferences.Default.Get(EndpointPreferenceKey, string.Empty);
        return Enum.TryParse<ApiEndpointKind>(value, out var kind) ? kind : GetDefaultEndpoint();
    }
}
