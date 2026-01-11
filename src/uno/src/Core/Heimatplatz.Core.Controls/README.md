# Heimatplatz.Core.Controls

Wiederverwendbare UI-Controls fuer die Heimatplatz App.

## Controls

### AppHeader
Header-Komponente mit Logo, Objektzaehler und Account-Button.

```xml
<controls:AppHeader ObjectCount="{Binding TotalCount}" />
```

### AppFooter
Footer mit Impressum, Datenschutz und Kontakt Links.

```xml
<controls:AppFooter />
```

### PropertyCard
Karte fuer Immobilien-Anzeige in Grid-Listen.

```xml
<controls:PropertyCard
    ImageUrl="{Binding HauptbildUrl}"
    Location="{Binding Ort}"
    Price="{Binding Preis}"
    Area="{Binding Wohnflaeche}"
    ExtraInfo="{Binding ZusatzInfo}"
    Command="{Binding TapCommand}" />
```

## Abhaengigkeiten

- Heimatplatz.Core.Styles (Design System)
- Heimatplatz.Shared (UnoService Konstanten)

## Registrierung

```csharp
services.AddControlsFeature();
```
