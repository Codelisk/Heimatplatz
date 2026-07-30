namespace Heimatplatz.Api.Features.Firmenbuch.Configuration;

/// <summary>
/// Konfiguration fuer die Firmenpool-API (eigener Firmenbuch-Spiegel, Repo AIRoutine/Firmenpool).
/// Der Firmenpool crawlt die amtliche FBW-HVD-Schnittstelle selbst und haelt zusaetzlich
/// Auszuege, Funktionaere, GISA-Gewerbe und Jahresabschluss-Kennzahlen - Heimatplatz fragt
/// live dort an und speichert selbst keine Firmenbuch-Daten mehr.
/// </summary>
public class FirmenpoolOptions
{
    public const string SectionName = "Firmenbuch:Firmenpool";

    /// <summary>
    /// Basis-URL der Firmenpool-API. Der Vorgabewert ist der Uebergangsbetrieb auf dem
    /// aiconnector-Server; dessen Caddy laesst die lesenden Firmendaten-Routen nur fuer
    /// die freigeschalteten IPs durch (Heimatplatz-Hetzner + Daniels Anschluss).
    /// </summary>
    public string BaseUrl { get; set; } = "https://static.91.18.104.178.clients.your-server.de";

    public int TimeoutSeconds { get; set; } = 60;
}
