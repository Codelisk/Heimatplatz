# MapLibreNative.Maui.Handlers (Vendor)

Gevendorter Quellcode des MAUI-Layers des MapLibre-Bindings: `MapLibreMap`-Control,
plattformspezifische Handler/Controller (Android/MaciOS/Windows), Sources/Layers/
Overlays. Die App (`Heimatplatz.Maui`) referenziert dieses Projekt; die
P/Invoke-Basis `MapLibreNative.Maui` kommt transitiv mit.

## Herkunft

- **Upstream:** [TechIdiots-LLC/MaplibreNativeMAUI](https://github.com/TechIdiots-LLC/MaplibreNativeMAUI)
- **Stand:** Tag `v4.5.0`, Commit `21fb356` (uebernommen am 28.07.2026, Ordner `handlers/`)
- **Lizenz:** BSD 2-Clause (siehe `LICENSE`, Copyright Andrew Calcutt)

## Bekannte Eigenheiten dieses Stands (Nutzung s. PropertyMapPage)

- `StyleUrl` mit Inline-JSON laedt still den Demotiles-Default — Stil per
  `Controller.SetStyleString()` nach MapReady setzen und warten, bis
  `GetStyleLayerIds()` den eigenen Stil zeigt (vorher verwerfen die Controller
  AddSource/AddLayer kommentarlos).
- Query-Bug: `QueryRenderedFeatures*` wertet Layer-min/maxZoom gegen einen falschen
  Zoom aus — interaktive Layer ohne Zoom-Grenzen anlegen, Sichtbarkeit ueber
  `["step",["zoom"],...]`-Opacity, Zoom-Weiche im Click-Handler.

## Lokale Abweichungen vom Upstream-Stand

1. `TargetFrameworks` auf net10 reduziert.
2. `ProjectReference`-Pfad auf `../MapLibreNative.Maui/` angepasst (Upstream-Ordner
   heisst `bindings/`).
3. `System.Text.Json`-PackageReference entfernt (net10-Framework-Bestandteil,
   bricht sonst als NU1510 den Build).

Update-Prozess: siehe README im Projekt `MapLibreNative.Maui`.
