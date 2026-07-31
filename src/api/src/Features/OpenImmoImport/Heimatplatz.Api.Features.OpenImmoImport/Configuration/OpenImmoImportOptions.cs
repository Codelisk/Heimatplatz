namespace Heimatplatz.Api.Features.OpenImmoImport.Configuration;

public class OpenImmoImportOptions
{
    public const string SectionName = "OpenImmoImport";

    /// <summary>
    /// Intervall des automatischen Ordner-Scans (OpenImmoImportWorker) in Minuten.
    /// 0 oder negativ (Default) deaktiviert den Worker - Integrationstests booten alle
    /// Hosted Services, daher MUSS der Default inaktiv sein (wie ScrapingOptions.SyncIntervalHours).
    /// </summary>
    public int ScanIntervalMinutes { get; set; }

    /// <summary>
    /// Wurzelverzeichnis der FTP-Drop-Ordner; je Feed liegt darunter ein Unterordner
    /// mit dem Feed-Key (= Home des chrooted FTP-Users, siehe deploy/hetzner/docker-compose.yml).
    /// Leer (Default) = Feature komplett inaktiv, auch fuer den manuellen Trigger.
    /// </summary>
    public string IncomingRootPath { get; set; } = "";

    /// <summary>
    /// Verzeichnis fuer API-eigenen Zustand (Marker-Files, Arbeitskopien). Muss AUSSERHALB
    /// der FTP-Chroots liegen, damit der Feed-Absender die Marker nicht sehen/loeschen kann.
    /// Null = "state" als Geschwister von IncomingRootPath.
    /// </summary>
    public string? StateRootPath { get; set; }

    /// <summary>
    /// Eine Feed-Datei gilt erst als vollstaendig hochgeladen, wenn ihr letzter
    /// Schreibzeitpunkt mindestens so viele Sekunden zurueckliegt (Schutz vor
    /// Partial-Uploads - FTP kennt kein atomares Rename-Protokoll auf Absenderseite).
    /// </summary>
    public int FileStableSeconds { get; set; } = 120;

    /// <summary>Maximale Zahl uebernommener Bilder pro Objekt (wie ScrapingOptions.MaxImagesPerAuction).</summary>
    public int MaxImagesPerProperty { get; set; } = 20;

    /// <summary>Harte Groessengrenze fuer ein einzelnes Bild/Attachment (Download, Base64, ZIP-Entry).</summary>
    public long MaxAttachmentBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>Zip-Bomb-Guard: maximale entpackte Gesamtgroesse eines Feed-ZIPs.</summary>
    public long MaxArchiveUncompressedBytes { get; set; } = 2_147_483_648;

    /// <summary>
    /// Deckel pro Import-Lauf fuer Nominatim-Geocoding (1 Request/Sekunde gedrosselt,
    /// wie ForeclosurePropertySyncService.MaxGeocodesPerSync). Betrifft nur Objekte
    /// ohne Geokoordinaten im Feed.
    /// </summary>
    public int MaxGeocodesPerRun { get; set; } = 25;

    /// <summary>
    /// False (Default): ein Feed mit 0 verwertbaren Objekten bricht ab statt den
    /// kompletten Bestand der Quelle zu loeschen (Schutz vor leerem/kaputtem Push,
    /// Analogon zum Leerlisten-Guard des ZV-Syncs).
    /// </summary>
    public bool AllowEmptySnapshot { get; set; }

    /// <summary>
    /// Shared-Key fuer Trigger- und Status-Endpoint (Header X-Sync-Key). Leer/nicht
    /// gesetzt = Endpoints sind ausserhalb von Development gesperrt (fail-closed).
    /// Wert kommt per Env OpenImmoImport__SyncTriggerKey (deploy/hetzner/.env, SYNC_TRIGGER_KEY).
    /// </summary>
    public string? SyncTriggerKey { get; set; }

    public List<OpenImmoFeedOptions> Feeds { get; set; } = [];

    /// <summary>Effektiver State-Pfad (Default: "state" neben IncomingRootPath).</summary>
    public string ResolveStateRootPath()
    {
        if (!string.IsNullOrWhiteSpace(StateRootPath))
            return StateRootPath;

        var parent = Path.GetDirectoryName(Path.GetFullPath(IncomingRootPath));
        return Path.Combine(parent ?? IncomingRootPath, "state");
    }
}

/// <summary>
/// Ein konfigurierter OpenImmo-Feed (z.B. ein Makler, der ueber Justimmo exportiert).
/// </summary>
public class OpenImmoFeedOptions
{
    /// <summary>Unterordner unter IncomingRootPath (= Home des FTP-Users) und Schluessel im State-Verzeichnis.</summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Property.SourceName dieser Quelle (Domain-Stil wie "immobaer.at", vgl.
    /// ForeclosureAuctionConstants.SourceName). Muss dauerhaft stabil bleiben -
    /// der Sync dedupliziert und loescht ueber (SourceName, SourceId).
    /// </summary>
    public string SourceName { get; set; } = "";

    /// <summary>Anzeigename des Anbieters (SellerName + SellerSource fuer den Ausschlussfilter).</summary>
    public string SellerName { get; set; } = "";

    /// <summary>
    /// Erlaubte Hosts fuer EXTERN-Bild-URLs (Suffix-Match wie ImageProxy:AllowedHosts,
    /// ".justimmo.at" matcht Subdomains). Leer = keine externen Downloads (Base64/ZIP
    /// funktionieren trotzdem).
    /// </summary>
    public List<string> AllowedImageHosts { get; set; } = [];
}
