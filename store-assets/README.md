# Store Assets - Heimatplatz

Alle Dateien die fuer die Store-Praesenz in Google Play und App Store benoetigt werden.

## Ordnerstruktur

```
store-assets/
├── google-play/
│   ├── icon/
│   │   └── icon-512x512.png              # App-Icon (fertig)
│   ├── feature-graphic/
│   │   └── feature-graphic-1024x500.png   # Feature Graphic (fertig)
│   └── screenshots/                       # Min. 2 Screenshots (TODO)
│       ├── 01-home.png                    # Startseite mit Immobilien
│       ├── 02-detail.png                  # Immobilien-Detailseite
│       ├── 03-filter.png                  # Filter/Suche
│       ├── 04-favorites.png               # Favoriten
│       └── 05-foreclosure.png             # Zwangsversteigerungen
│
├── app-store/
│   ├── icon/
│   │   └── icon-1024x1024.png            # App-Icon (fertig)
│   ├── screenshots-6.7-inch/             # iPhone 15 Pro Max (TODO)
│   │   └── (1290x2796 px)
│   └── screenshots-5.5-inch/             # iPhone 8 Plus (TODO)
│       └── (1242x2208 px)
│
└── feature-graphic.html                   # Quell-HTML fuer Feature Graphic
```

## Status

| Asset | Google Play | App Store |
|-------|------------|-----------|
| Icon | icon-512x512.png | icon-1024x1024.png |
| Feature Graphic | feature-graphic-1024x500.png | - |
| Screenshots | TODO | TODO |
| Metadaten-Texte | cake/fastlane/metadata/android/de-DE/ | cake/fastlane/metadata/de-DE/ |

## Screenshots erstellen

Screenshots muessen manuell von einem echten Geraet oder Simulator aufgenommen werden.

### Google Play (min. 2, empfohlen 4-8)
- Format: 16:9 oder 9:16 (Portrait empfohlen)
- Min. 320px, max. 3840px pro Seite
- PNG oder JPEG

### App Store
- **6.7 Zoll** (iPhone 15 Pro Max): 1290 x 2796 px - PFLICHT
- **5.5 Zoll** (iPhone 8 Plus): 1242 x 2208 px - PFLICHT

### Empfohlene Screens fuer Screenshots
1. **Startseite** - Immobilienliste mit Karten
2. **Detailansicht** - Immobilie mit Fotos und Details
3. **Filter** - Suchfilter geoeffnet
4. **Favoriten** - Gespeicherte Immobilien
5. **Zwangsversteigerungen** - Auktionsliste

### Via iOS Simulator
```bash
xcrun simctl io booted screenshot screenshot.png
```

### Via iPad
Seitentaste + Lautstaerke hoch gleichzeitig druecken.

## Play Console - Manuelle Schritte

Folgende Daten muessen direkt in der Play Console eingetragen werden:

1. **App-Kategorie**: Haus & Garten oder Lifestyle
2. **Kontakt-E-Mail**: info@heimatplatz.at (oder datenschutz@heimatplatz.at)
3. **Datenschutzerklaerung-URL**: https://heimatplatz.at/datenschutz
4. **Inhaltsbewertung**: Fragebogen in Play Console ausfuellen (IARC)
5. **Zielgruppe und Inhalt**: App ist nicht fuer Kinder bestimmt
6. **Datensicherheit**: Fragebogen in Play Console ausfuellen

## Fastlane Metadata Upload

Sobald die App nicht mehr im Draft-Status ist:
```bash
cd cake && SUPPLY_JSON_KEY="$(pwd)/secrets/play-store-key.json" fastlane android update_metadata
```
