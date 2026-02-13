# Heimatplatz.Core.Styles

Minimales, flexibles Design-System basierend auf WinUI/Fluent.

## Zweck

- Anpassbare Farbpalette via `ThemeOverrides.xaml`
- Spacing-Tokens fuer konsistente Abstaende
- Responsive Layout-Helpers
- Light/Dark/HighContrast Theme Unterstuetzung

## Struktur

```
Core.Styles/
├── Styles.xaml              # Einstiegspunkt (in App.xaml referenzieren)
├── ThemeOverrides.xaml      # ANPASSBARE FARBPALETTE
├── Tokens/
│   └── Spacing.xaml         # Spacing-Tokens (SpacingSm, SpacingMd, etc.)
└── Layouts/
    └── Responsive.xaml      # Responsive Helpers (utu:Responsive)
```

## Verwendung

In `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            <ToolkitResources xmlns="using:Uno.Toolkit.UI" />
            <ResourceDictionary Source="ms-appx:///Heimatplatz.Core.Styles/Styles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Farben anpassen

Oeffne `ThemeOverrides.xaml` und aendere die Farben:

```xml
<!-- Light Theme -->
<ResourceDictionary x:Key="Light">
    <!-- Deine Markenfarbe -->
    <Color x:Key="SystemAccentColor">#005FB8</Color>

    <!-- Hintergrund -->
    <Color x:Key="SolidBackgroundFillColorBase">#F3F3F3</Color>

    <!-- Text -->
    <Color x:Key="TextFillColorPrimary">#1A1A1A</Color>
</ResourceDictionary>
```

## Verfuegbare Brushes

| Brush | Verwendung |
|-------|------------|
| `AccentBrush` | Primaere Akzentfarbe |
| `BackgroundBrush` | Seiten-Hintergrund |
| `SurfaceBrush` | Cards, Panels |
| `CardBrush` | Card-Hintergrund |
| `OnBackgroundBrush` | Text auf Hintergrund |
| `OnSurfaceBrush` | Text auf Surface |
| `OnSurfaceVariantBrush` | Sekundaerer Text |
| `BorderBrush` | Rahmen |
| `DividerBrush` | Trennlinien |
| `ErrorBrush` | Fehler |
| `SuccessBrush` | Erfolg |
| `WarningBrush` | Warnung |
| `InfoBrush` | Information |

## Spacing Tokens

```xml
<x:Double x:Key="SpacingXs">4</x:Double>
<x:Double x:Key="SpacingSm">8</x:Double>
<x:Double x:Key="SpacingMd">16</x:Double>
<x:Double x:Key="SpacingLg">24</x:Double>
<x:Double x:Key="SpacingXl">32</x:Double>
<x:Double x:Key="SpacingXxl">48</x:Double>

<CornerRadius x:Key="RadiusSm">4</CornerRadius>
<CornerRadius x:Key="RadiusMd">8</CornerRadius>
```

## Control Styling

Nutze die eingebauten WinUI/Toolkit Styles direkt:

```xml
<!-- WinUI Standard Buttons -->
<Button Content="Primary" Style="{StaticResource AccentButtonStyle}" />
<Button Content="Secondary" />

<!-- Uno Toolkit -->
<utu:Card>
    <TextBlock Text="Content" />
</utu:Card>
```

Fuer Anpassungen: Lightweight Styling verwenden (Farb-Overrides statt eigene Templates).

## Responsive Layouts

```xml
xmlns:utu="using:Uno.Toolkit.UI"

<!-- Responsive Padding -->
<Grid Padding="{utu:Responsive Narrowest=16, Normal=24, Wide=32}">

<!-- Responsive Visibility -->
<Border Visibility="{utu:Responsive Narrowest=Collapsed, Normal=Visible}" />
```

## Best Practices

1. **Keine hardcoded Farben** - Nutze `{ThemeResource ...}`
2. **Keine eigenen ControlTemplates** - Lightweight Styling nutzen
3. **Spacing-Tokens verwenden** - Konsistente Abstaende
4. **WinUI Styles nutzen** - Nicht neu erfinden

## Abhaengigkeiten

- Uno.Toolkit.UI (fuer Responsive Markup Extension)
- Microsoft.UI.Xaml (WinUI Controls)
