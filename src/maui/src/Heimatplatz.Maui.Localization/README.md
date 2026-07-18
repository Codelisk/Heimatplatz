# Heimatplatz.Maui.Localization

Zentrale Lokalisierung der MAUI-App. Alle user-sichtbaren UI-Texte liegen als
`.resx`-Ressourcen in diesem Projekt (aktuell **rein deutsch**, neutrale Ressource
ohne Locale-Suffix). Der Source Generator `Shiny.Extensions.Localization.Generator`
erzeugt daraus pro `.resx` eine strongly-typed `{Name}Localized`-Klasse sowie die
DI-Registrierung `AddStronglyTypedLocalizations()`.

## Warum ein eigenes Projekt?

Gleicher Grund wie beim `Heimatplatz.Maui.ApiClient`: Source-Generatoren sehen
die Ausgabe anderer Source-Generatoren nicht. Die MAUI-App kompiliert XAML samt
Bindings per Source Generator (`MauiXamlInflator=SourceGen`) - Bindings auf
`*Localized`-Typen funktionieren nur, wenn diese Typen als normale
Assembly-Referenz vorliegen.

## Konventionen

- Pro Seite/Feature-Slice eine Marker-Klasse + gleichnamige `.resx` im selben
  Ordner (Pflicht des Generators: `.resx` braucht gleichnamige Klasse im
  selben Namespace; Ordnerstruktur = Namespace).
- Feature-uebergreifende Texte (OK, Abbrechen, Fehler, ...) in `CommonStrings`.
- Format-Strings (`{0}`) erzeugen Methoden mit `Format`-Suffix statt Properties.
- Keys in PascalCase; Umlaute im Value, nie im Key.

## Verwendung

```csharp
// App-Start (MauiProgram)
builder.Services.AddStronglyTypedLocalizations();

// ViewModel: Localized-Klasse injizieren und als Loc exponieren
public HomeStringsLocalized Loc { get; }
```

```xml
<!-- XAML: Binding auf das Loc-Property des ViewModels -->
<Label Text="{Binding Loc.EmptyTitle}" />
```

## Abhaengigkeiten

- `Microsoft.Extensions.Localization` (Laufzeit, `IStringLocalizer<T>`)
- `Shiny.Extensions.Localization.Generator` (nur Compile-Zeit)
