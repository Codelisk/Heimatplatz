# Figma Design Dokumentation

## Design System

### Farben

| Name | Hex | Verwendung |
|------|-----|------------|
| Schwarz | `#000000` | Header, Primär-Buttons, aktive Filter |
| Weiß | `#FFFFFF` | Hintergrund, Karten, Button-Text |
| Grau Hell | `#F5F5F5` | Suchleiste, Input-Felder |
| Grau Mittel | `#E0E0E0` | Bild-Placeholder, Borders |
| Grau Text | `#666666` | Sekundärer Text (Adressen) |
| Grau Light | `#999999` | Tertiärer Text (Details) |

### Typografie

| Element | Font | Size | Weight |
|---------|------|------|--------|
| Logo | Inter | 20px | Bold |
| Preis | Inter | 20px | Bold |
| Navigation | Inter | 14px | Medium |
| Adresse | Inter | 14px | Regular |
| Details | Inter | 12px | Regular |
| Button | Inter | 16px | Semi Bold |

### Abstände

- Padding Container: 16px (Mobile), 80px (Desktop)
- Card Padding: 16px
- Element Spacing: 8px, 12px, 16px, 24px
- Corner Radius: 8px (Buttons, Chips), 12px (Cards)

---

## Mobile Design (375 x 812px)

### Struktur

```
Mobile Frame (4:55)
├── Header (schwarz, 64px)
│   ├── Logo "ImmoOÖ"
│   └── Button "Anmelden"
│
├── Suchleiste (grau, abgerundet)
│   └── Placeholder "Ort, PLZ oder Region..."
│
├── Filter Chips (horizontal)
│   ├── "Haus" (aktiv/schwarz)
│   ├── "Grundstück"
│   └── "Zwangsversteigerung"
│
├── Immobilien Card
│   ├── Bild Placeholder (grau)
│   ├── Preis "€ 349.000"
│   ├── Adresse "Einfamilienhaus..."
│   └── Details "120 m² · 4 Zimmer"
│
└── Buttons
    ├── "Alle Ergebnisse anzeigen" (primär/schwarz)
    └── "Filter anpassen" (sekundär/weiß)
```

---

## Desktop Design (1440 x 900px)

### Struktur

```
Desktop Frame (4:56)
├── Header (schwarz, 64px, volle Breite)
│   ├── Logo "ImmoOÖ"
│   ├── Link "Immobilien anbieten"
│   └── Button "Anmelden"
│
├── Suchleiste (600px, zentriert)
│   └── Placeholder "Ort, PLZ oder Region..."
│
├── Filter Chips (horizontal, zentriert)
│   ├── "Haus" (aktiv)
│   ├── "Grundstück"
│   ├── "Zwangsversteigerung"
│   ├── "Preis"
│   ├── "Größe"
│   └── "Zimmer"
│
├── Cards Grid (3 Spalten, 24px gap)
│   ├── Card 1: Linz-Urfahr (€ 349.000)
│   ├── Card 2: Wels (€ 189.000)
│   └── Card 3: Gmunden (€ 520.000)
│
└── Button "Mehr laden" (zentriert)
```

### Beispiel-Immobilien

| # | Preis | Ort | Details |
|---|-------|-----|---------|
| 1 | € 349.000 | Einfamilienhaus in Linz-Urfahr | 145 m² · 5 Zimmer · Haus |
| 2 | € 189.000 | Baugrundstück in Wels | 850 m² · Grundstück |
| 3 | € 520.000 | Villa in Gmunden am Traunsee | 220 m² · 7 Zimmer · Villa |

---

## Screens Status

- [x] Detailansicht (Immobilie) - Mobile & Desktop
- [x] Filter-Modal (erweiterte Filter) - Mobile & Desktop
- [x] Login / Registrierung - Mobile & Desktop
- [x] Account-Seite - Mobile & Desktop
- [x] Favoriten - Mobile & Desktop
- [x] Anbieter-Dashboard - Mobile & Desktop
- [ ] Objekt erstellen/bearbeiten (optional)
