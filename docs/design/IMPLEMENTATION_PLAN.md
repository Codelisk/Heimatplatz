# Heimatplatz - Implementierungsplan

## Analyse-Zusammenfassung

### Design-Konzept
- **App-Typ:** Immobilien-Plattform für Oberösterreich
- **Zielgruppe:** Private Käufer und Verkäufer in Oberösterreich
- **Sprache:** Nur Deutsch (keine Lokalisierung)
- **Design-Stil:** Minimalistisch, Schwarz-Weiß mit Akzenten
- **Responsive:** Mobile-First mit Desktop-Layouts

### Identifizierte Screens (aus Figma-Exports)
| Screen | Mobile | Desktop | Priorität |
|--------|--------|---------|-----------|
| Startseite (Suche + Liste) | ✅ | ✅ | P0 |
| Detailansicht (Immobilie) | ✅ | ✅ | P0 |
| Login | ✅ | ✅ | P1 |
| Registrierung | ✅ | ✅ | P1 |
| Account/Profil | ✅ | ✅ | P1 |
| Immobilie anlegen | ✅ | ✅ | P2 |
| Startseite (eingeloggt) | - | ✅ | P1 |

### Design-System (aus DESIGN.md)
```
Farben:
- Primary: #000000 (Schwarz)
- Background: #F8FAFC (Hellgrau)
- Surface: #FFFFFF (Weiß)
- Border: #E2E8F0
- Text Primary: #0F172A
- Text Secondary: #666666
- Text Tertiary: #999999
- Accent/CTA: #2563EB (Blau - nur Detailansicht)

Typografie:
- Font: Inter
- Logo: 24-32px Bold
- Preis: 20px Bold
- Body: 14px Regular
- Details: 12px Regular
- Button: 16px SemiBold

Abstände:
- Container Padding: 16px (Mobile), 80px (Desktop)
- Card Corner Radius: 8px
- Button Corner Radius: 6px
- Spacing: 8px, 12px, 16px, 24px
```

---

## Architektur-Entscheidungen

### 1. Responsive Strategie
**Entscheidung:** `ResponsiveView` mit NarrowTemplate/WideTemplate

**Begründung:**
- Design zeigt komplett unterschiedliche Layouts (Mobile: 1 Card, Desktop: 3-Grid)
- Bestehende MainPage nutzt bereits `{utu:Responsive}` für Spacing
- Uno Toolkit ResponsiveView ermöglicht saubere Trennung

**Breakpoints:**
- Narrow: < 800px (Mobile/Tablet Portrait)
- Wide: >= 800px (Desktop/Tablet Landscape)

### 2. Navigation
**Entscheidung:** Einfache Frame-Navigation (kein NavigationView/TabBar)

**Begründung:**
- Design zeigt keine TabBar oder Sidebar
- Header-basierte Navigation mit "Anmelden" / "Zurück"
- Später erweiterbar auf TabBar für eingeloggte User

### 3. Feature-Struktur
```
src/uno/src/Features/
├── Properties/           # Immobilien-Feature (Suche, Liste, Detail)
├── Auth/                 # Login, Registrierung, Account
└── Listings/             # Immobilie anlegen (Anbieter)

src/api/src/Features/
├── Properties/           # Immobilien CRUD, Suche
├── Auth/                 # Authentifizierung
└── Users/                # User-Profile
```

### 4. Theme-Anpassung
**Entscheidung:** Light Theme mit angepasster Farbpalette

**Änderungen in ThemeOverrides.xaml:**
- RequestedTheme: Light (aktuell Dark)
- SystemAccentColor: #000000 (Schwarz statt Blau)
- Background: #F8FAFC
- Surface/Card: #FFFFFF
- Border: #E2E8F0

---

## Implementierungsplan

### Phase 1: Foundation (Core Setup)

#### 1.1 Theme & Design System anpassen
**Dateien:**
- `src/uno/src/Core/Heimatplatz.Core.Styles/ThemeOverrides.xaml`
- `src/uno/src/Heimatplatz.App/App.xaml` (RequestedTheme: Light)

**Aufgaben:**
- [ ] RequestedTheme auf "Light" ändern
- [ ] Farbpalette gemäß Design-System anpassen
- [ ] Neue semantische Brushes definieren:
  - `HeaderBackgroundBrush` (#FFFFFF mit Border)
  - `ChipSelectedBrush` (#000000)
  - `ChipUnselectedBrush` (#FFFFFF)
  - `PrimaryButtonBrush` (#000000)
  - `SecondaryButtonBrush` (#FFFFFF mit Border)

#### 1.2 Responsive Layout Helpers
**Dateien:**
- `src/uno/src/Core/Heimatplatz.Core.Styles/Layouts/Responsive.xaml`

**Aufgaben:**
- [ ] DefaultResponsiveLayout mit Breakpoints definieren (Narrow: 300, Wide: 800)
- [ ] Container-Styles für Mobile/Desktop Padding

#### 1.3 Reusable Controls erstellen
**Neues Projekt:** `Heimatplatz.Core.Controls`

**Controls:**
```
Controls/
├── AppHeader.xaml              # Header mit Logo + Navigation
├── PropertyCard.xaml           # Immobilien-Karte
├── FilterChip.xaml             # Selektierbarer Filter-Chip
├── FilterChipGroup.xaml        # Gruppe von Chips
├── LocationPicker.xaml         # Ort-Auswahl Dropdown
└── MenuItem.xaml               # Account-Menü Item
```

---

### Phase 2: Properties Feature (Immobilien)

#### 2.1 API: Properties Feature
**Projekte:**
- `Heimatplatz.Api.Features.Properties`
- `Heimatplatz.Api.Features.Properties.Contracts`

**Entities:**
```csharp
public class Property : BaseEntity
{
    public string Title { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public decimal Price { get; set; }
    public int? LivingAreaM2 { get; set; }
    public int? LandAreaM2 { get; set; }
    public int? Rooms { get; set; }
    public int? YearBuilt { get; set; }
    public PropertyType Type { get; set; }  // Haus, Grundstueck, Zwangsversteigerung
    public SellerType SellerType { get; set; }  // Privat, Makler
    public string SellerName { get; set; }
    public string Description { get; set; }
    public List<string> Features { get; set; }  // Garage, Garten, etc.
    public List<string> ImageUrls { get; set; }
}

public enum PropertyType { Haus, Grundstueck, Zwangsversteigerung }
public enum SellerType { Privat, Makler }
```

**Endpoints:**
- `GET /api/properties` - Liste mit Filter (Type, Preis, Größe, Ort)
- `GET /api/properties/{id}` - Einzelne Immobilie

#### 2.2 Uno: Properties Feature
**Projekte:**
- `Heimatplatz.Features.Properties`
- `Heimatplatz.Features.Properties.Contracts`

**Screens:**
```
Presentation/
├── HomePage.xaml                # Startseite mit Suche + Liste
├── HomePage.xaml.cs
├── HomeViewModel.cs
├── PropertyDetailPage.xaml      # Detailansicht
├── PropertyDetailPage.xaml.cs
└── PropertyDetailViewModel.cs
```

**HomePage Layout (ResponsiveView):**
```xml
<utu:ResponsiveView>
    <utu:ResponsiveView.NarrowTemplate>
        <!-- Mobile: Vertical Stack -->
        <!-- Header, LocationPicker, FilterChips, Single Card, Buttons -->
    </utu:ResponsiveView.NarrowTemplate>
    <utu:ResponsiveView.WideTemplate>
        <!-- Desktop: Header full-width, Content centered max-width -->
        <!-- LocationPicker, FilterChips, 3-Column Grid, Load More -->
    </utu:ResponsiveView.WideTemplate>
</utu:ResponsiveView>
```

---

### Phase 3: Auth Feature

#### 3.1 API: Auth Feature
**Projekte:**
- `Heimatplatz.Api.Features.Auth`
- `Heimatplatz.Api.Features.Auth.Contracts`

**Endpoints:**
- `POST /api/auth/login` - Login mit Email/Password
- `POST /api/auth/register` - Registrierung
- `GET /api/auth/me` - Aktueller User

#### 3.2 Uno: Auth Feature
**Projekte:**
- `Heimatplatz.Features.Auth`
- `Heimatplatz.Features.Auth.Contracts`

**Screens:**
```
Presentation/
├── LoginPage.xaml
├── LoginViewModel.cs
├── RegisterPage.xaml
├── RegisterViewModel.cs
├── AccountPage.xaml
└── AccountViewModel.cs
```

---

### Phase 4: Listings Feature (Anbieter)

#### 4.1 API: Listings
- `POST /api/properties` - Immobilie erstellen (Auth required)
- `PUT /api/properties/{id}` - Immobilie bearbeiten
- `DELETE /api/properties/{id}` - Immobilie löschen
- `GET /api/properties/my` - Meine Immobilien

#### 4.2 Uno: Listings
**Screens:**
- `CreatePropertyPage.xaml` - Immobilie anlegen
- `MyPropertiesPage.xaml` - Meine Inserate

---

## Datei-Mapping: Design → Implementation

| Design Export | Uno Page | ViewModel |
|---------------|----------|-----------|
| MobileStartseite.xaml | HomePage.xaml (NarrowTemplate) | HomeViewModel |
| DesktopStartseite.xaml | HomePage.xaml (WideTemplate) | HomeViewModel |
| MobileDetailansicht.xaml | PropertyDetailPage.xaml (NarrowTemplate) | PropertyDetailViewModel |
| DesktopDetailansicht.xaml | PropertyDetailPage.xaml (WideTemplate) | PropertyDetailViewModel |
| LoginMobile.xaml | LoginPage.xaml (NarrowTemplate) | LoginViewModel |
| LoginDesktop.xaml | LoginPage.xaml (WideTemplate) | LoginViewModel |
| RegistrierungMobile.xaml | RegisterPage.xaml (NarrowTemplate) | RegisterViewModel |
| RegistrierungDesktop.xaml | RegisterPage.xaml (WideTemplate) | RegisterViewModel |
| AccountMobile.xaml | AccountPage.xaml (NarrowTemplate) | AccountViewModel |
| AccountDesktop.xaml | AccountPage.xaml (WideTemplate) | AccountViewModel |
| ImmobilieAnlegenMobile.xaml | CreatePropertyPage.xaml (NarrowTemplate) | CreatePropertyViewModel |
| ImmobilieAnlegenDesktop.xaml | CreatePropertyPage.xaml (WideTemplate) | CreatePropertyViewModel |

---

## Reihenfolge der Implementierung

### Sprint 1: Core + Startseite
1. Theme anpassen (Light, Farben)
2. AppHeader Control erstellen
3. PropertyCard Control erstellen
4. FilterChip/FilterChipGroup Controls erstellen
5. API: Properties Feature (GET Liste)
6. Uno: HomePage mit ResponsiveView
7. Seeding: 10+ Beispiel-Immobilien

### Sprint 2: Detail + Navigation
1. LocationPicker Control
2. API: Properties Feature (GET Detail)
3. Uno: PropertyDetailPage
4. Navigation: Home → Detail → Back

### Sprint 3: Auth
1. API: Auth Feature
2. Uno: LoginPage
3. Uno: RegisterPage
4. Uno: AccountPage
5. Header: Anmelden → Mein Konto (wenn eingeloggt)

### Sprint 4: Anbieter
1. API: Listings Endpoints
2. Uno: CreatePropertyPage
3. Uno: MyPropertiesPage
4. Dashboard-Funktionen

---

## Technische Hinweise

### XAML-Konvertierung aus Design-Exports
Die exportierten XAML-Dateien verwenden absolute Positionierung (Margin-basiert).
Diese müssen konvertiert werden zu:
- `AutoLayout` / `StackPanel` für Flows
- `Grid` mit RowDefinitions für strukturierte Layouts
- `{utu:Responsive}` für responsive Werte
- Theme-Ressourcen statt Hardcoded-Farben

### Beispiel-Konvertierung:
```xml
<!-- VORHER (Export) -->
<TextBlock Text="€ 349.000"
           FontFamily="Inter"
           FontWeight="Bold"
           FontSize="20"
           Foreground="Black"/>

<!-- NACHHER (Uno Best Practice) -->
<TextBlock Text="{Binding Price, StringFormat='€ {0:N0}'}"
           Style="{StaticResource TitleLargeTextStyle}"
           Foreground="{ThemeResource OnSurfaceBrush}"/>
```

### Navigation Pattern
```csharp
// In ViewModel (mit Shiny.Mediator)
public async Task NavigateToDetail(Property property)
{
    await _navigator.NavigateAsync<PropertyDetailPage>(new { Id = property.Id });
}
```

---

## Offene Fragen

1. **Bilder:** Woher kommen die Immobilien-Bilder? (Upload vs. URL)
2. **Karte:** Soll die Karte in der Detailansicht interaktiv sein?
3. **Kontakt:** Wie funktioniert "Anbieter kontaktieren"? (Email, In-App-Chat, Formular)
4. **Favoriten:** Sollen Favoriten auch ohne Login funktionieren? (LocalStorage)
5. **Suchprofile:** Was genau sind "Suchprofile" im Account-Bereich?
