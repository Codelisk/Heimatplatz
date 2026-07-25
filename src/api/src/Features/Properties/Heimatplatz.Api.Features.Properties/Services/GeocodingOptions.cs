namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Konfiguration fuer das Adress-Geocoding (Abschnitt "Geocoding").
/// </summary>
public class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>
    /// Opt-in: ohne explizite Aktivierung gehen keine Requests an den externen
    /// Geocoder raus (Tests/CI bleiben offline, Seeds bringen eigene Koordinaten).
    /// Aktiv in appsettings.Development.json und per Env-Var auf den Servern.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Nominatim-kompatible Basis-URL (selbst hostbar).</summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>Nominatim-Policy verlangt einen identifizierenden User-Agent mit Kontakt.</summary>
    public string UserAgent { get; set; } = "Heimatplatz/1.0 (info@heimatplatz.at)";

    /// <summary>Mindestabstand zwischen zwei Requests (Nominatim-Policy: max. 1/Sekunde).</summary>
    public int MinRequestIntervalMs { get; set; } = 1100;
}
