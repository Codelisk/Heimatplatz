# Heimatplatz.Shared (Uno)

Uno-spezifische Shared-Komponenten für die Heimatplatz-Lösung.

## Zweck

Dieses Projekt enthält gemeinsam genutzte Konstanten und Utilities für alle Uno-Projekte.

## Inhalt

### UnoService

Stellt DI-Konstanten für die automatische Service-Registrierung via `Shiny.Extensions.DependencyInjection` bereit.

```csharp
using Heimatplatz;

[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class MyService : IMyService { }
```

| Eigenschaft | Wert | Beschreibung |
|-------------|------|--------------|
| `Lifetime` | `Singleton` | Eine Instanz für die gesamte App-Lebensdauer |
| `TryAdd` | `true` | Verhindert doppelte Registrierungen |

## Abhängigkeiten

- `Shiny.Extensions.DependencyInjection` - Automatische Service-Registrierung
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI-Abstractions
