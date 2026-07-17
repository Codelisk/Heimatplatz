# Hetzner-Deployment

Docker-Compose-Setup fuer Heimatplatz auf einem einzelnen Hetzner-Server (API + Test-API + PostgreSQL + Astro-Web-SSR + Caddy als Reverse-Proxy mit automatischem HTTPS).

## Astro-Web (SSR, Container `web`)

Seit 15.7.2026 laeuft das Web-Frontend als SSR-Node-Server (vorher statisches file_server-Hosting): `node:22-alpine` fuehrt das Standalone-Bundle des `@astrojs/node`-Adapters aus (`/srv/heimatplatz-web/server/entry.mjs`, read-only gemountet). Seiten werden pro Request gerendert - Immobilien-Daten sind immer aktuell, der fruehere 6h-Rebuild-Schedule entfaellt. SSR-Fetches gehen ueber `API_BASE_URL_SERVER=http://api:8080` direkt ins interne Docker-Netz.

Deploy: `deploy-apps.yml` (Target `DeployAstro`) baut das Bundle in CI, rsynct `dist/` nach `/srv/heimatplatz-web` sowie `docker-compose.yml`/`Caddyfile` nach `/srv/heimatplatz/deploy/hetzner` (kein Git-Checkout am Server!) und fuehrt danach per SSH `docker compose up -d web`, `docker compose restart web` und einen Caddy-Reload aus (siehe `cake/Tasks/DeployAstroTask.cs`).

## Interner Bereich (`/intern`) + `HOME_IP`

`/intern` auf `WEB_DOMAIN` (Admin-UI: manueller Edikte-Sync-Trigger) und
`POST /api/foreclosure-auctions/sync` auf `API_DOMAIN` sind in Caddy per `remote_ip`-Matcher
auf `HOME_IP` beschraenkt (Server-`.env`, Leerzeichen-getrennt IPs/CIDR-Ranges - siehe
`.env.example`). Alles andere bekommt `403`. Bei geaenderter Heim-IP: `HOME_IP` in der
Server-`.env` aktualisieren und `docker compose restart caddy`.

Zusaetzlich verlangt der Sync-Endpoint den Shared-Key-Header `X-Sync-Key`
(`SYNC_TRIGGER_KEY` in der Server-`.env`, siehe `.env.example`) - noetig, weil die
Test-API keine IP-Sperre hat. Der Web-Container schickt den Key automatisch mit
(`/intern/sync.ts`); ohne konfigurierten Key ist der Endpoint gesperrt (fail-closed).
**Beim naechsten Deploy `SYNC_TRIGGER_KEY` in der Server-`.env` ergaenzen**, sonst
verweigert der manuelle Sync-Trigger den Dienst.

**Status:** LIVE seit 8.7.2026. Server `heimatplatz` (CX23, Nuernberg, `128.140.33.238`), Projekt "Vorleistung" in der Hetzner Console. Stack laeuft unter `/srv/heimatplatz/deploy/hetzner`, API erreichbar via `https://api.heimatplatz.at/health`. `.github/workflows/deploy-hetzner.yml` ist weiterhin inaktiv (Deploy bisher manuell auf dem Server). SSH: `root@128.140.33.238` (Key-only; hinterlegt sind der `vorleistung-key` und Daniels `id_ed25519`).

## Erstinbetriebnahme (sobald ein Server existiert)

1. Repo auf den Server klonen (oder nur `deploy/hetzner/` + `src/api/` + die Root-`Directory.*.props`/`nuget.config` - der Docker-Build braucht den vollen Kontext, siehe `src/api/Dockerfile`).
2. `cp deploy/hetzner/.env.example deploy/hetzner/.env` und echte Werte eintragen:
   - `POSTGRES_PASSWORD`: langes Zufalls-Passwort
   - `Authentication__Jwt__Key`: `openssl rand -base64 64` - niemals den alten kompromittierten oder den git-getrackten Dev-Key verwenden
   - `API_DOMAIN`: die tatsaechliche Domain (DNS muss vorher auf den Server zeigen, damit Caddy das Let's-Encrypt-Zertifikat bekommt)
3. `docker compose -f deploy/hetzner/docker-compose.yml up -d --build`
4. `curl https://<API_DOMAIN>/health` sollte `{"Status":"Healthy"}` liefern.

## Test-API (api-test in docker-compose.yml)

Seit 14.7.2026: `https://test-api.heimatplatz.at` - gleicher Code/Build wie die Prod-API, verbunden mit der Testdatenbank (Port 5433). Seeding ist aktiv: Nach einem Test-DB-Reset genuegt `docker compose restart api-test`, dann wird automatisch migriert und neu geseedet. Eigener JWT-Key (`TEST_JWT_KEY` in der Server-`.env`), damit Test-Tokens nicht auf Prod gelten. DNS-A-Record `test-api` liegt in der Hetzner-DNS-Zone (verwaltbar via Cloud-API mit `HCLOUD_TOKEN`).

Benoetigte Variablen in der Server-`.env`: `TEST_API_DOMAIN=test-api.heimatplatz.at`, `TEST_JWT_KEY` (`openssl rand -base64 64`), `TESTDB_PASSWORD` (wie bisher), `APNS_TEAM_ID`, `APNS_KEY_ID` und `APNS_BUNDLE_ID=at.heimatplatz.app`.

Der APNs Private Key wird nicht ins Image eingebaut. Er liegt auf dem Server unter
`/srv/heimatplatz/deploy/hetzner/secrets/apns-auth-key.p8` und wird fuer `api-test`
read-only nach `/run/secrets/apns-auth-key.p8` gemountet. Das Verzeichnis `secrets/`
ist gitignoriert.

## Test-Web (web-test in docker-compose.yml)

Seit 16.7.2026: `https://test.heimatplatz.at` - gleiche Astro-SSR-Laufzeit wie das
Prod-Web (`web`), haengt aber an der Test-API (`api-test`) statt der Prod-API. Oeffentlich
erreichbar, aber per `X-Robots-Tag: noindex, nofollow` (Caddyfile) von der Suchmaschinen-
Indexierung ausgenommen (gleiche Inserate wie Prod = kein Duplicate-Content).

Zwei Ebenen muessen auf die Test-API zeigen:

- **SSR-Fetches** (Detailseiten, Listen-Rendering): zur Laufzeit ueber
  `API_BASE_URL_SERVER=http://api-test:8080` (Container-Env, internes Docker-Netz).
- **Client-Skripte** (Live-Suche, Anlegen/Bearbeiten): die `PUBLIC_API_BASE_URL` wird beim
  Build ins JS-Bundle eingebettet - das Test-Bundle wird daher **separat** mit
  `PUBLIC_API_BASE_URL=https://test-api.heimatplatz.at` gebaut und liegt unter
  `/srv/heimatplatz-web-test` (getrennt vom Prod-Bundle in `/srv/heimatplatz-web`).

Deploy: `deploy-apps.yml` Target **`DeployAstroTest`** (Cake) baut das Bundle gegen die
Test-API (`Web:ApiBaseUrlTest` in `cake/appsettings.json`) und rsynct es nach
`Hetzner:WebRootTest` (`/srv/heimatplatz-web-test`), dann `docker compose up -d web-test`
+ Caddy-Reload. Bewusst getrennt von `DeployAstro`/`DeployAll`: erst Test deployen +
pruefen, dann Prod nachziehen. Benoetigte Server-`.env`-Variable: `WEB_TEST_DOMAIN=test.heimatplatz.at`.

DNS-A-Record `test` liegt in der Hetzner-DNS-Zone (Cloud-API mit `HCLOUD_TOKEN`,
`POST /v1/zones/1433807/rrsets`). Caddy loest `{$WEB_TEST_DOMAIN}` aus seiner eigenen
Container-Env auf - beim Hinzufuegen der Variable muss der **Caddy-Container neu erstellt**
werden (`docker compose up -d caddy`), ein reiner Reload sieht die neue Env nicht.

## Testdatenbank (docker-compose.testdb.yml)

Separater Postgres-16-Container fuer Entwicklung/Tests auf demselben Server - unabhaengig vom Prod-Stack, eigenes Volume, von aussen erreichbar auf **Port 5433** (UFW-Regel vorhanden; Schutz ueber starkes Passwort, `TESTDB_PASSWORD` in der Server-`.env` und der lokalen `deploy/hetzner/.env`).

```
Host=128.140.33.238;Port=5433;Database=heimatplatz_test;Username=heimatplatz_test;Password=<TESTDB_PASSWORD>
```

Lokale Dev-Umgebung umstellen (User-Secrets, nicht git-getrackt):

```
dotnet user-secrets set "Database:Provider" "Postgres" --project src/api/src/Heimatplatz.Api/Heimatplatz.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<Connection-String oben>" --project src/api/src/Heimatplatz.Api/Heimatplatz.Api.csproj
```

Zurueck auf lokales SQLite: `dotnet user-secrets clear --project src/api/src/Heimatplatz.Api/Heimatplatz.Api.csproj` (dann greift wieder `appsettings.Development.json`). Beim ersten API-Start mit `AutoMigrate=true`/`EnableSeeding=true` (Development-Default) werden Migrationen und Seed-Daten automatisch eingespielt.

Test-DB zuruecksetzen (auf dem Server):

```
cd /srv/heimatplatz/deploy/hetzner
docker compose -f docker-compose.testdb.yml down -v   # loescht auch das Volume
docker compose -f docker-compose.testdb.yml up -d
```

## Datenuebernahme aus Azure SQL

Noch nicht automatisiert. Die neue Postgres-Instanz startet leer (essentielle Referenzdaten wie Bundeslaender/Impressum werden per Seeder beim ersten Start automatisch angelegt, siehe `Database__EnableSeeding=false` in `docker-compose.yml` - das betrifft nur die Demo-/Test-Seeder, nicht die essentiellen). Ein Datenexport aus der bestehenden Azure-SQL-Produktionsdatenbank (echte Nutzer/Inserate) ist ein separater Schritt, der vor dem finalen Cutover geplant werden muss.

## Migrationen

Postgres hat eine eigene Migrations-Historie (`src/api/src/Core/Heimatplatz.Api.Core.Data.Migrations.Postgres/`), getrennt von der bestehenden SQLite/SQL-Server-Historie in `Heimatplatz.Api.Core.Data/Migrations/`. Neue Migration hinzufuegen:

```
dotnet ef migrations add <Name> \
  --project src/api/src/Core/Heimatplatz.Api.Core.Data.Migrations.Postgres/Heimatplatz.Api.Core.Data.Migrations.Postgres.csproj \
  --startup-project src/api/src/Core/Heimatplatz.Api.Core.Data.Migrations.Postgres/Heimatplatz.Api.Core.Data.Migrations.Postgres.csproj \
  --context Heimatplatz.Api.Core.Data.AppDbContext \
  --output-dir Migrations
```

Wichtig: `--startup-project` muss auf das Migrations.Postgres-Projekt SELBST zeigen (nicht auf `Heimatplatz.Api`) - sonst nutzt `dotnet ef` die per DI konfigurierte Laufzeit-Datenbank (SQLite/SQL Server) statt der Postgres-Design-Time-Factory.
