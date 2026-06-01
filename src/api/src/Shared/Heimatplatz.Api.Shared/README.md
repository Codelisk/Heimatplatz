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

### Cleanup/IUserDataEraser

Contributor-Interface (Namespace `Heimatplatz.Api.Cleanup`) fuer die feature-uebergreifende
Konto-Loeschung. Jedes Feature mit benutzerbezogenen Daten implementiert einen Eraser und
registriert ihn explizit per `services.AddScoped<IUserDataEraser, ...>()`. Der zentrale
`DeleteAccountHandler` (Auth-Feature) ruft alle registrierten Eraser nach `Order` auf.
Dadurch bleibt das Auth-Feature von anderen Features entkoppelt.

## Abhängigkeiten

- `Shiny.Extensions.DependencyInjection` - Automatische Service-Registrierung
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI-Abstractions
