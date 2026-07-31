# Heimatplatz.Api.Features.OpenImmoImport

Importiert Makler-Objektbestaende aus **OpenImmo-XML-Feeds** (1.2.6/1.2.7) in die
Property-Tabelle. Erster Anwendungsfall: **Immobär** (immobaer.at) exportiert ueber
**Justimmo** per FTP-Push - Justimmo laedt mehrmals taeglich den kompletten
Objektbestand (Vollbestand, kein Delta) als XML/ZIP auf unseren FTP-Server
(Container `ftp` in `deploy/hetzner/docker-compose.yml`), dieser Feature scannt
den Drop-Ordner und synchronisiert.

## Ablauf eines Laufs (OpenImmoImportService)

1. Neueste `*.xml`/`*.zip` im Feed-Ordner `{IncomingRootPath}/{feedKey}` suchen.
2. Stabilitaets-Check: Datei-mtime muss aelter als `FileStableSeconds` sein
   (FTP-Upload evtl. noch aktiv).
3. Marker-Kurzschluss: `{StateRootPath}/{feedKey}/last-import.json` kennt die
   zuletzt importierte Datei (Name/Groesse/mtime/SHA256) - unveraendert = NoOp.
4. Arbeitskopie nach `{StateRootPath}/{feedKey}/work/` (Justimmo darf die
   Drop-Datei jederzeit ueberschreiben), SHA256-Vergleich.
5. Parsen (`OpenImmoParser`, lenient, namespace-agnostisch) - Produktfilter direkt
   dort: nur **KAUF** + Objektart **haus/grundstueck/land_und_forstwirtschaft**,
   nur Objekte **mit Kaufpreis** ("auf Anfrage" wird nicht importiert).
6. Sync (`OpenImmoPropertySyncService`, Vorbild ForeclosurePropertySyncService):
   Upsert ueber `(SourceName, SourceId)`, Snapshot-Diff-Loeschung verschwundener
   Objekte (**ausser IsHidden** - Moderation ueberlebt), leerer Vollbestand bricht
   ab (`AllowEmptySnapshot=false`), unveraenderte Zeilen werden nicht angefasst
   (kein UpdatedAt-Bump, kein Delta-Journal-Rauschen), Feed-Geokoordinaten
   bevorzugt vor Nominatim (gedeckelt `MaxGeocodesPerRun`),
   `PropertyCreatedEvent` je Neuzugang (Push-Benachrichtigungen).
7. Bilder (`OpenImmoImageService`): EXTERN-URL (Host-Allowlist!), Base64 oder
   ZIP-Entry → `wwwroot/uploads/openimmo/{feedKey}/{safeSourceId}/` als Original
   `{sha20}{ext}` + Display-Variante `{sha20}.display.jpg` (ImageDisplayVariant),
   `manifest.json` je Objekt verhindert Re-Downloads.
8. Marker schreiben (nur bei Erfolg - kaputte Dateien werden beim naechsten Tick
   erneut versucht).

## Endpoints

| Endpoint | Zweck |
|----------|-------|
| `POST /api/openimmo-import/sync` | Manueller Trigger (fire-and-forget, `Force` umgeht Marker) |
| `GET /api/openimmo-import/status` | Letzter Lauf + Bestand je Feed |

Beide via Shared-Key `X-Sync-Key` (`OpenImmoImport:SyncTriggerKey`, fail-closed
ausserhalb Development) plus Caddy-IP-Sperre auf Prod.

## Konfiguration (Section `OpenImmoImport`)

| Option | Default | Bedeutung |
|--------|---------|-----------|
| `ScanIntervalMinutes` | `0` (aus) | Intervall des Hintergrund-Scans (`OpenImmoImportWorker`) |
| `IncomingRootPath` | leer (aus) | Wurzel der FTP-Drop-Ordner, je Feed ein Unterordner |
| `StateRootPath` | `state` neben Incoming | Marker/Arbeitskopien, AUSSERHALB der FTP-Chroots |
| `FileStableSeconds` | `120` | Upload-Abschluss-Erkennung |
| `MaxImagesPerProperty` | `20` | Bild-Deckel pro Objekt |
| `MaxAttachmentBytes` | 20 MB | Groessencap pro Bild |
| `MaxArchiveUncompressedBytes` | 2 GB | Zip-Bomb-Guard |
| `MaxGeocodesPerRun` | `25` | Nominatim-Deckel pro Lauf |
| `AllowEmptySnapshot` | `false` | Leerer Vollbestand loescht NICHT den Bestand |
| `SyncTriggerKey` | leer (gesperrt) | Shared-Key fuer die Endpoints |
| `Feeds[].Key` | - | Unterordner + State-Schluessel (z.B. `immobaer`) |
| `Feeds[].SourceName` | - | `Property.SourceName`, dauerhaft stabil (z.B. `immobaer.at`) |
| `Feeds[].SellerName` | - | Anbietername (SellerSource → Ausschlussfilter) |
| `Feeds[].AllowedImageHosts` | leer | Host-Allowlist fuer EXTERN-Bild-URLs (`.justimmo.at`-Stil) |

Produktion: `deploy/hetzner/docker-compose.yml` setzt `IncomingRootPath`,
`StateRootPath`, `ScanIntervalMinutes` und den Trigger-Key per Env; das
`openimmo_data`-Volume teilen sich `ftp`- und `api`-Container.

## Produktentscheidungen

- **Keine Wohnungen, kein Gewerbe, keine Miete** (Produktregel; Datenmodell kennt
  nur House/Land/Foreclosure und keinen Mietpreis) - solche Objekte zaehlen als
  "uebersprungen" und stehen im Log/Status.
- **"Preis auf Anfrage" wird nicht importiert** (Preis 0 saehe in Listen/Filtern
  kaputt aus).
- `SourceName` im Domain-Stil (`immobaer.at`) - das Web zeigt Inserate mit
  `SourceName != "Heimatplatz"` bereits als externe Quelle an.
- System-User: dieselbe GUID wie der ZV-Sync (`OpenImmoImportConstants`).

## Abhaengigkeiten

- `Core.Data` (AppDbContext), `Shared` (SharedKeyAuthorization, ImageDisplayVariant)
- `Features.Properties` (+Contracts): Property-Entity, ISellerInfoResolver, IPropertyGeocoder
- `Features.Locations`: Municipality-Aufloesung
- `Features.Auth`: IPasswordHasher (System-User)
- `Features.Notifications.Contracts`: PropertyCreatedEvent
