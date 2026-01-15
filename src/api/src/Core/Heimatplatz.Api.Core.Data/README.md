# Heimatplatz.Api.Core.Data

Zentrales Datenzugriffs-Projekt mit Entity Framework Core DbContext und Basis-Entities.

## Zweck

- Bereitstellung des zentralen `AppDbContext` für alle Features
- Definition der `BaseEntity` Basisklasse für alle Entities
- Multi-Provider Unterstützung (SQLite, PostgreSQL, SQL Server)
- Automatische Timestamp-Verwaltung (CreatedAt, UpdatedAt)

## Öffentliche APIs

### AppDbContext

Der zentrale DbContext mit dynamischer Entity-Configuration-Registrierung:

```csharp
// Registrierung von Entity-Configurations aus Feature-Assemblies
AppDbContext.RegisterConfigurations(typeof(MyEntityConfiguration).Assembly);
```

### BaseEntity

Abstrakte Basisklasse für alle Entities:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

### ServiceCollectionExtensions

DI-Registrierung:

```csharp
services.AddAppData(configuration);
```

## Abhängigkeiten

- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Sqlite
- Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.EntityFrameworkCore.SqlServer

## Konfiguration

In `appsettings.json`:

```json
{
  "Database": {
    "Provider": "sqlite"  // sqlite, postgresql, sqlserver
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```
