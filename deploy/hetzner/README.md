# Hetzner-Deployment (Vorbereitung)

Docker-Compose-Setup fuer die Heimatplatz-API auf einem einzelnen Hetzner-Server (API + PostgreSQL + Caddy als Reverse-Proxy mit automatischem HTTPS).

**Status:** Vorbereitung. Es existiert noch kein Server; `.github/workflows/deploy-hetzner.yml` ist inaktiv (siehe Kommentar dort).

## Erstinbetriebnahme (sobald ein Server existiert)

1. Repo auf den Server klonen (oder nur `deploy/hetzner/` + `src/api/` + die Root-`Directory.*.props`/`nuget.config` - der Docker-Build braucht den vollen Kontext, siehe `src/api/Dockerfile`).
2. `cp deploy/hetzner/.env.example deploy/hetzner/.env` und echte Werte eintragen:
   - `POSTGRES_PASSWORD`: langes Zufalls-Passwort
   - `Authentication__Jwt__Key`: `openssl rand -base64 64` - niemals den alten kompromittierten oder den git-getrackten Dev-Key verwenden
   - `API_DOMAIN`: die tatsaechliche Domain (DNS muss vorher auf den Server zeigen, damit Caddy das Let's-Encrypt-Zertifikat bekommt)
3. `docker compose -f deploy/hetzner/docker-compose.yml up -d --build`
4. `curl https://<API_DOMAIN>/health` sollte `{"Status":"Healthy"}` liefern.

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
