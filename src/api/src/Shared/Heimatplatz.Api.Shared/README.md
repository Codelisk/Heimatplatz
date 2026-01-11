# Heimatplatz.Api.Shared

API-spezifische Shared-Komponenten für die Heimatplatz-Lösung.

## Zweck

Dieses Projekt enthält gemeinsam genutzte Konstanten und Utilities für alle API-Projekte.

## Inhalt

### ApiService

Stellt DI-Konstanten für die automatische Service-Registrierung via `Shiny.Extensions.DependencyInjection` bereit.

```csharp
using Heimatplatz.Api;

[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MyService : IMyService { }
```

| Eigenschaft | Wert | Beschreibung |
|-------------|------|--------------|
| `Lifetime` | `Scoped` | Pro HTTP-Request eine Instanz |
| `TryAdd` | `true` | Verhindert doppelte Registrierungen |

## Abhängigkeiten

- `Shiny.Extensions.DependencyInjection` - Automatische Service-Registrierung
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI-Abstractions
