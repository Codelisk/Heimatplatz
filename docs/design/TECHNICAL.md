# Technische Details

## Figma Node IDs

Diese IDs können verwendet werden um die Elemente via MCP zu referenzieren.

### Frames

| Element | Node ID | Größe |
|---------|---------|-------|
| Mobile Frame | `4:55` | 375 x 812px |
| Desktop Frame | `4:56` | 1440 x 900px |

### Mobile Elemente

| Element | Node ID | Parent |
|---------|---------|--------|
| Header | `5:57` | 4:55 |
| Logo Text | `5:58` | 5:57 |
| Anmelden Button | `5:59` | 5:57 |
| Suchleiste | `5:60` | 4:55 |
| Filter Container | `5:62` | 4:55 |
| Chip Haus | `5:63` | 5:62 |
| Chip Grundstück | `5:65` | 5:62 |
| Chip Zwangsversteigerung | `5:67` | 5:62 |
| Immobilien Card | `5:69` | 4:55 |
| Button Primär | `5:74` | 4:55 |
| Button Sekundär | `5:75` | 4:55 |

### Desktop Elemente

| Element | Node ID | Parent |
|---------|---------|--------|
| Header | `5:78` | 4:56 |
| Suchleiste | `5:82` | 4:56 |
| Filter Chips Container | `5:83` | 4:56 |
| Cards Grid | `5:97` | 4:56 |
| Card 1 | `5:98` | 5:97 |
| Card 2 | `5:99` | 5:97 |
| Card 3 | `5:100` | 5:97 |
| Button Mehr laden | `5:113` | 4:56 |

### Card 1 Inhalt (5:98)

| Element | Node ID |
|---------|---------|
| Bild Placeholder | `5:101` |
| Preis | `5:104` |
| Adresse | `5:107` |
| Details | `5:110` |

### Card 2 Inhalt (5:99)

| Element | Node ID |
|---------|---------|
| Bild Placeholder | `5:102` |
| Preis | `5:105` |
| Adresse | `5:108` |
| Details | `5:111` |

### Card 3 Inhalt (5:100)

| Element | Node ID |
|---------|---------|
| Bild Placeholder | `5:103` |
| Preis | `5:106` |
| Adresse | `5:109` |
| Details | `5:112` |

---

## Zusätzliche Screens

### Detailansicht

| Element | Node ID | Größe |
|---------|---------|-------|
| Detailansicht Mobile | `5:114` | 375 x 812px |
| Detailansicht Desktop | `5:115` | 1440 x 900px |

### Filter Modal

| Element | Node ID | Größe |
|---------|---------|-------|
| Filter Modal Mobile | `5:200` | 375 x 812px |
| Filter Modal Desktop | `5:201` | 1440 x 900px |

### Login / Registrierung

| Element | Node ID | Größe |
|---------|---------|-------|
| Login Mobile | `5:250` | 375 x 812px |
| Login Desktop | `5:251` | 1440 x 900px |

### Account / Profil

| Element | Node ID | Größe |
|---------|---------|-------|
| Account Mobile | `5:300` | 375 x 812px |
| Account Desktop | `5:301` | 1440 x 900px |

### Favoriten

| Element | Node ID | Größe |
|---------|---------|-------|
| Favoriten Mobile | `5:366` | 375 x 812px |
| Favoriten Desktop | `5:367` | 1440 x 900px |

### Anbieter Dashboard

| Element | Node ID | Größe |
|---------|---------|-------|
| Dashboard Mobile | `5:402` | 375 x 812px |
| Dashboard Desktop | `5:403` | 1440 x 900px |

---

## MCP Server Konfiguration

Die `.mcp.json` im Projekt-Root:

```json
{
  "mcpServers": {
    "uno": {
      "type": "http",
      "url": "https://mcp.platform.uno/v1"
    },
    "figma-mcp": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "http://localhost:38450/mcp"]
    }
  }
}
```

### Verwendete MCP Tools

**Figma Design erstellen:**
- `mcp__figma-mcp__create-frame` - Frames erstellen
- `mcp__figma-mcp__create-rectangle` - Rechtecke (Placeholder, Backgrounds)
- `mcp__figma-mcp__create-text` - Textelemente
- `mcp__figma-mcp__set-fill-color` - Füllfarbe setzen
- `mcp__figma-mcp__set-stroke-color` - Rahmenfarbe setzen
- `mcp__figma-mcp__set-corner-radius` - Abrundung setzen
- `mcp__figma-mcp__set-layout` - Auto-Layout (horizontal/vertical)
- `mcp__figma-mcp__move-node` - Elemente verschieben
- `mcp__figma-mcp__delete-node` - Elemente löschen

**FigJam Diagramme:**
- `mcp__plugin_figma_figma__generate_diagram` - Mermaid.js Diagramme

---

## Nächste Schritte (Technisch)

1. **Uno Platform Projekt erstellen**
   - `dotnet new unoapp -n Heimatplatz`
   - MVUX für State Management
   - Material Design Theme

2. **Komponenten implementieren**
   - ImmobilienCard UserControl
   - FilterChip Component
   - SearchBar Component

3. **Navigation einrichten**
   - Shell mit NavigationView
   - Seiten: Home, Detail, Filter, Account

4. **Backend anbinden**
   - API Service für Immobilien-Daten
   - Authentication Service
