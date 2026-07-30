namespace Heimatplatz.Api.Features.Firmenbuch.Configuration;

/// <summary>
/// Konfiguration fuer die Firmenpool-API (eigener Firmenbuch-Spiegel, Repo AIRoutine/Firmenpool).
/// Der Firmenpool crawlt die amtliche FBW-HVD-Schnittstelle selbst und haelt zusaetzlich
/// Auszuege, Funktionaere, GISA-Gewerbe und Jahresabschluss-Kennzahlen - Heimatplatz zieht
/// daraus nur noch den Katalog-Stammsatz und crawlt die Justiz-Schnittstelle nicht mehr selbst.
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

    /// <summary>Seitengroesse beim Abziehen des Katalogs (Firmenpool deckelt bei 200).</summary>
    public int PageSize { get; set; } = 200;

    /// <summary>
    /// Shared-Key fuer den EIGENEN Katalog-Sync-Trigger (Header X-Sync-Key, fail-closed).
    /// Faellt per PostConfigure auf den historischen Schluessel Firmenbuch:Hvd:SyncTriggerKey
    /// zurueck, damit das bestehende Deployment (env Firmenbuch__Hvd__SyncTriggerKey)
    /// unveraendert weiterlaeuft.
    /// </summary>
    public string? SyncTriggerKey { get; set; }
}
