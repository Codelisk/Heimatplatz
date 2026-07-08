# Heimatplatz.Api.Core.Data.Seeding

Zentrale Seeding-Infrastruktur fuer Referenz- und Mock-Daten.

## Verwendung

### 1. Seeder im Feature erstellen

```csharp
using Heimatplatz.Api.Core.Data.Seeding;

public class MyFeatureSeeder(AppDbContext dbContext) : ISeeder
{
    public int Order => 10; // Optional: Reihenfolge

    // Optional: false fuer essentielle Referenzdaten, die auch in Produktion
    // gebraucht werden (Default: true = Demo-Daten, laufen nur bei EnableSeeding=true)
    public bool IsDemoData => false;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: Nur seeden wenn leer
        if (await dbContext.MyEntities.AnyAsync(cancellationToken))
            return;

        dbContext.MyEntities.AddRange(
            new MyEntity { Name = "Test 1" },
            new MyEntity { Name = "Test 2" }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### 2. Seeder registrieren

In der Feature `ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddMyFeature(this IServiceCollection services)
{
    services.AddSeeder<MyFeatureSeeder>();
    return services;
}
```

### 3. Seeders ausfuehren

Die Seeders werden automatisch beim API-Start ausgefuehrt (`SeederRunner.RunAllSeedersAsync`,
aufgerufen aus `Core.Startup.InitializeDatabaseAsync`):

- **Essentielle Seeder** (`IsDemoData == false`) laufen in JEDER Umgebung (auch Produktion)
- **Demo-Seeder** (`IsDemoData == true`, Default) laufen NUR bei `Database:EnableSeeding=true`

## Wichtig

- **Idempotent**: Seeder muessen pruefen ob Daten bereits existieren
- **Order**: Niedrigere Zahlen werden zuerst ausgefuehrt (fuer Abhaengigkeiten)
- **Keine Loeschung**: Daten werden nur hinzugefuegt, nie geloescht
- **IsDemoData**: Default `true` - nur bewusst auf `false` setzen, wenn die Daten
  echte Referenzdaten sind, die in Produktion gebraucht werden
