# Heimatplatz.Api.Core.Data.Migrations.Postgres

EF-Core-Migrations-Assembly fuer den **Postgres**-Provider (Produktion und Test-System
auf Hetzner). Enthaelt ausschliesslich Migrations + Model-Snapshot, keine eigene Logik.

## Warum ein eigenes Projekt?

Migrations sind provider-spezifisch. Lokal laeuft die API auf SQLite (ohne Migrations,
`EnsureCreated`), Prod/Test auf Postgres - die Postgres-Migrations duerfen deshalb nicht
im provider-neutralen `Heimatplatz.Api.Core.Data` liegen. Der Provider-Switch bindet die
Assembly explizit ein (`ServiceCollectionExtensions` in `Core.Data`:
`UseNpgsql(..., x => x.MigrationsAssembly(...))`).

## Neue Migration erzeugen

```bash
dotnet ef migrations add <Name> \
  --project src/api/src/Core/Heimatplatz.Api.Core.Data.Migrations.Postgres \
  --startup-project src/api/src/Heimatplatz.Api \
  -- --Database:Provider Postgres
```

Angewendet werden Migrations beim API-Start automatisch, wenn `Database:AutoMigrate=true`
(siehe `Heimatplatz.Api.Core.Startup`).

## Abhaengigkeiten

- `Heimatplatz.Api.Core.Data` (AppDbContext, Entities via Auto-Discovery)
- `Npgsql.EntityFrameworkCore.PostgreSQL`
