# Heimatplatz.Features.Immobilien

Immobilien-Feature fuer die Heimatplatz Uno App.

## Inhalt

### Presentation
- `ImmobilienPage` - Uebersichtsseite mit Filter und Grid
- `ImmobilienViewModel` - ViewModel fuer die Uebersicht
- `ImmobilieDetailPage` - Detailseite einer Immobilie
- `ImmobilieDetailViewModel` - ViewModel fuer die Detailseite

### Controls
- `FilterBar` - Filterleiste fuer Typ, Preis, Ort, Flaeche

## API Integration

Verwendet die generierten HTTP Clients aus `Core.ApiClient`:
- `GetImmobilienHttpRequest` - Paginierte Liste mit Filtern
- `GetImmobilieByIdHttpRequest` - Einzelne Immobilie
- `GetImmobilienAnzahlHttpRequest` - Gesamtzahl fuer Header

## Abhaengigkeiten

- Heimatplatz.Features.Immobilien.Contracts
- Heimatplatz.Core.Controls (AppHeader, AppFooter, PropertyCard)
- Heimatplatz.Core.ApiClient (Generated HTTP Clients)
- Heimatplatz.Core.Styles (Design System)

## Registrierung

```csharp
services.AddImmobilienFeature();
```

## Navigation Routes

```csharp
new RouteMap("Immobilien", View: views.FindByViewModel<ImmobilienViewModel>())
new RouteMap("ImmobilieDetail", View: views.FindByViewModel<ImmobilieDetailViewModel>())
```
