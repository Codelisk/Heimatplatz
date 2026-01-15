# FigJam - Konzept & User Flows

## FigJam URL

https://www.figma.com/online-whiteboard/create-diagram/2e255d19-3f3d-4d44-9b40-6988f3ca2724

## User Flows

### Käufer (Buyers)

```
Käufer
├── Anonym (ohne Account)
│   ├── Suchen & Filtern
│   ├── Ergebnisse ansehen
│   └── Details ansehen
│
└── Registriert (mit Account)
    ├── Alle anonymen Features
    ├── Favoriten speichern
    ├── Suchprofile anlegen
    └── Kontakt zu Anbietern
```

### Anbieter (Sellers)

```
Anbieter
├── Privatperson
│   ├── Einfache Registrierung
│   ├── Eigene Objekte inserieren
│   └── Anfragen verwalten
│
└── Unternehmen (Makler)
    ├── Firmen-Account
    ├── Mehrere Objekte verwalten
    ├── Team-Mitglieder
    └── Statistiken & Analytics
```

## Filter-System

### Basis-Filter (immer sichtbar)
- Immobilienart: Haus, Grundstück, Zwangsversteigerung
- Preis: Min/Max Slider
- Größe: m² Bereich
- Zimmer: Anzahl

### Erweiterte Filter
- Standort: Bezirk, Gemeinde
- Anbietertyp: Privat / Makler
- Baujahr
- Ausstattung

## Mermaid Diagramm (Original)

```mermaid
flowchart LR
    subgraph Kaeufer["Käufer"]
        K1["Anonym"]
        K2["Registriert"]
    end

    subgraph App["Heimatplatz App"]
        A1["Startseite"]
        A2["Suche & Filter"]
        A3["Ergebnisliste"]
        A4["Detailansicht"]
        A5["Favoriten"]
        A6["Kontakt"]
    end

    subgraph Anbieter["Anbieter"]
        V1["Privatperson"]
        V2["Unternehmen"]
    end

    subgraph Filter["Filter-System"]
        F1["Haus"]
        F2["Grundstück"]
        F3["Zwangsversteigerung"]
        F4["Preis"]
        F5["Größe"]
        F6["Privat/Makler"]
    end

    subgraph Dashboard["Anbieter-Dashboard"]
        D1["Objekte verwalten"]
        D2["Anfragen"]
        D3["Statistiken"]
    end

    K1 --> A1
    K2 --> A1
    A1 --> A2
    A2 --> Filter
    Filter --> A3
    A3 --> A4
    K2 --> A5
    A4 --> A6

    V1 --> Dashboard
    V2 --> Dashboard
    Dashboard --> A3
```
