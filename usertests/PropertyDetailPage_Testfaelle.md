# PropertyDetailPage - Testfälle

## Übersicht

Diese Testfälle prüfen, ob die PropertyDetailPage nur die für den jeweiligen Immobilientyp relevanten Felder anzeigt.

## Immobilientypen

| Typ | Enum-Wert | Beschreibung |
|-----|-----------|--------------|
| **Haus** | `House = 1` | Einfamilienhaus, Reihenhaus, Wohnung, etc. |
| **Grundstück** | `Land = 2` | Baugrundstück, Ackerland, etc. |
| **Zwangsversteigerung** | `Foreclosure = 3` | Versteigerungsobjekt (kann Haus oder Grundstück sein) |

---

## Felddefinitionen

### Felder die IMMER angezeigt werden müssen

| Feld | UI-Element | Beschreibung |
|------|------------|--------------|
| **Titel** | `ViewModel.Property.Title` | Überschrift der Immobilie |
| **Preis** | `ViewModel.FormattedPrice` | Kaufpreis in Euro (z.B. "250.000 €") |
| **Grundstücksfläche** | `ViewModel.PlotAreaText` | Fläche des Grundstücks in m² |
| **Adresse** | `ViewModel.AddressText` | Vollständige Adresse (Straße, PLZ, Ort) |
| **Anbieter** | `ViewModel.Property.SellerName` | Name des Verkäufers |
| **Bilder** | `ImageFlipView` | Bildergalerie mit Counter |

### Felder die NUR bei HAUS angezeigt werden dürfen

| Feld | UI-Element | Beschreibung |
|------|------------|--------------|
| **Wohnfläche** | `ViewModel.Property.LivingAreaM2` | Wohnfläche in m² |
| **Zimmer** | `ViewModel.Property.Rooms` | Anzahl der Zimmer |
| **Baujahr** | `ViewModel.YearBuiltText` | Jahr der Errichtung |
| **Preis/m²** | `ViewModel.PricePerSqmText` | Preis pro Quadratmeter Wohnfläche |

### Felder die bei GRUNDSTÜCK NICHT angezeigt werden dürfen

| Feld | Grund |
|------|-------|
| **Wohnfläche** | Grundstück hat keine Wohnfläche |
| **Zimmer** | Grundstück hat keine Zimmer |
| **Baujahr** | Grundstück hat kein Baujahr |
| **Preis/m²** (Wohnfläche) | Nur bei Grundstücksfläche sinnvoll |

---

## Testfälle

### TC-PD-001: Grundstück - Keine gebäudespezifischen Felder

**Vorbedingung:**
- App ist gestartet
- Benutzer ist eingeloggt (optional)
- Es existiert mindestens ein Grundstück (PropertyType = Land)

**Schritte:**
1. Navigiere zur HomePage
2. Filtere nach "Grund" (nur Grundstücke anzeigen)
3. Klicke auf eine Grundstücks-Karte
4. PropertyDetailPage öffnet sich

**Erwartetes Ergebnis:**

| Prüfpunkt | Erwartet |
|-----------|----------|
| Titel | Sichtbar |
| Preis | Sichtbar |
| Grundstücksfläche | Sichtbar mit Wert in m² |
| Adresse | Sichtbar |
| Anbieter | Sichtbar |
| **Wohnfläche** | **NICHT sichtbar oder "-"** |
| **Zimmer** | **NICHT sichtbar oder keine Anzeige "X Zimmer"** |
| **Baujahr** | **NICHT sichtbar oder "-"** |
| **Preis/m²** | **NICHT sichtbar oder basiert auf Grundstücksfläche** |

**Screenshot-Validierung:**
- Im Visual Tree darf KEIN Element mit Text "X Zimmer" vorhanden sein
- Das Element "Baujahr" darf nicht sichtbar sein oder "-" anzeigen
- Die Zeile "Wohnfläche · Zimmer" darf nicht angezeigt werden

---

### TC-PD-002: Haus - Alle gebäudespezifischen Felder vorhanden

**Vorbedingung:**
- App ist gestartet
- Benutzer ist eingeloggt (optional)
- Es existiert mindestens ein Haus (PropertyType = House)

**Schritte:**
1. Navigiere zur HomePage
2. Filtere nach "Haus" (nur Häuser anzeigen)
3. Klicke auf eine Haus-Karte
4. PropertyDetailPage öffnet sich

**Erwartetes Ergebnis:**

| Prüfpunkt | Erwartet |
|-----------|----------|
| Titel | Sichtbar |
| Preis | Sichtbar |
| Grundstücksfläche | Sichtbar mit Wert in m² |
| Adresse | Sichtbar |
| Anbieter | Sichtbar |
| **Wohnfläche** | **Sichtbar mit Wert in m²** |
| **Zimmer** | **Sichtbar mit Anzahl (z.B. "4 Zimmer")** |
| **Baujahr** | **Sichtbar mit Jahreszahl (z.B. "1985")** |
| **Preis/m²** | **Sichtbar, berechnet aus Preis/Wohnfläche** |

**Screenshot-Validierung:**
- Im Visual Tree MUSS ein Element mit Text "X Zimmer" vorhanden sein
- Das Element "Baujahr" MUSS eine Jahreszahl anzeigen
- Die Zeile mit Wohnfläche und Zimmer MUSS sichtbar sein

---

### TC-PD-003: Zwangsversteigerung Grundstück - Keine gebäudespezifischen Felder

**Vorbedingung:**
- App ist gestartet
- Es existiert eine Zwangsversteigerung vom Typ Grundstück

**Schritte:**
1. Navigiere zur HomePage
2. Filtere nach "ZV" (nur Zwangsversteigerungen)
3. Identifiziere ein Grundstück-ZV (Badge zeigt "GRUND")
4. Klicke auf die Karte
5. PropertyDetailPage öffnet sich

**Erwartetes Ergebnis:**
- Wie TC-PD-001: Keine Zimmer, kein Baujahr, keine Wohnfläche sichtbar

---

### TC-PD-004: Zwangsversteigerung Haus - Alle gebäudespezifischen Felder

**Vorbedingung:**
- App ist gestartet
- Es existiert eine Zwangsversteigerung vom Typ Haus

**Schritte:**
1. Navigiere zur HomePage
2. Filtere nach "ZV" (nur Zwangsversteigerungen)
3. Identifiziere ein Haus-ZV (Badge zeigt "HAUS")
4. Klicke auf die Karte
5. PropertyDetailPage öffnet sich

**Erwartetes Ergebnis:**
- Wie TC-PD-002: Zimmer, Baujahr und Wohnfläche müssen sichtbar sein

---

## UI-Elemente Referenz (aus XAML)

### Mobile Layout - Relevante Elemente

```xml
<!-- Wohnfläche und Zimmer - Zeile 106-114 -->
<TextBlock FontSize="16">
    <Run Text="{x:Bind ViewModel.Property.LivingAreaM2}" />
    <Run Text=" m²" />
    <Run Text="  ·  " />
    <Run Text="{x:Bind ViewModel.Property.Rooms}" />
    <Run Text=" Zimmer" />
</TextBlock>

<!-- Baujahr - Zeile 147-155 -->
<StackPanel Grid.Column="2" Spacing="4">
    <TextBlock Text="Baujahr" />
    <TextBlock Text="{x:Bind ViewModel.YearBuiltText}" />
</StackPanel>
```

### Desktop Layout - Relevante Elemente

```xml
<!-- Wohnfläche und Zimmer - Zeile 427-435 -->
<TextBlock FontSize="15">
    <Run Text="{x:Bind ViewModel.Property.LivingAreaM2}" />
    <Run Text=" m²" />
    <Run Text="  ·  " />
    <Run Text="{x:Bind ViewModel.Property.Rooms}" />
    <Run Text=" Zimmer" />
</TextBlock>

<!-- Baujahr - Zeile 471-479 -->
<StackPanel Grid.Column="2" Spacing="4">
    <TextBlock Text="Baujahr" />
    <TextBlock Text="{x:Bind ViewModel.YearBuiltText}" />
</StackPanel>
```

---

## Testdaten Beispiele

### Grundstück (Land)

```json
{
  "Type": "Land",
  "Title": "Sonniges Baugrundstück Linz-Land",
  "Price": 245000,
  "PlotAreaM2": 720,
  "LivingAreaM2": null,
  "Rooms": null,
  "YearBuilt": null
}
```

### Haus (House)

```json
{
  "Type": "House",
  "Title": "Einfamilienhaus mit Garten",
  "Price": 450000,
  "PlotAreaM2": 850,
  "LivingAreaM2": 145,
  "Rooms": 5,
  "YearBuilt": 1998
}
```

---

## Automatisierte Validierung

Bei der Screenshot-Analyse sollen folgende Checks durchgeführt werden:

### Für Grundstück:
1. Visual Tree durchsuchen nach TextBlock mit "Zimmer" → **Darf nicht vorhanden sein**
2. Visual Tree durchsuchen nach "Baujahr" Label → **Wert muss "-" sein oder Element collapsed**
3. Visual Tree durchsuchen nach "Wohnfläche" → **Darf nicht vorhanden sein**

### Für Haus:
1. Visual Tree durchsuchen nach TextBlock mit "Zimmer" → **Muss vorhanden sein mit Zahl > 0**
2. Visual Tree durchsuchen nach "Baujahr" → **Muss Jahreszahl enthalten (4 Ziffern)**
3. Visual Tree durchsuchen nach Wohnfläche → **Muss vorhanden sein mit Wert in m²**

---

## Bekannte Issues

> **Aktueller Stand:** Die PropertyDetailPage zeigt derzeit ALLE Felder unabhängig vom Immobilientyp an. Dies ist ein bekannter Bug, der durch diese Testfälle validiert und später behoben werden soll.

### Betroffene Codezeilen:
- `PropertyDetailPage.xaml` Zeilen 106-114 (Mobile: Wohnfläche/Zimmer)
- `PropertyDetailPage.xaml` Zeilen 147-155 (Mobile: Baujahr)
- `PropertyDetailPage.xaml` Zeilen 427-435 (Desktop: Wohnfläche/Zimmer)
- `PropertyDetailPage.xaml` Zeilen 471-479 (Desktop: Baujahr)

### Lösung:
Diese Elemente müssen mit einer Visibility-Binding versehen werden:
```xml
Visibility="{x:Bind ViewModel.IsHouseType, Mode=OneWay}"
```

Oder alternativ über einen Converter:
```xml
Visibility="{x:Bind ViewModel.Property.Type,
             Converter={StaticResource PropertyTypeToVisibilityConverter},
             ConverterParameter='House'}"
```
