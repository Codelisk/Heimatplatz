# Heimatplatz.Core.DeepLink

Deep Linking Feature fuer die Heimatplatz App.

## Zweck

Ermoeglicht das Oeffnen der App ueber Custom URL Schemes und die Navigation zur entsprechenden Property-Detailseite.

## URL-Schema

| URL | Zielseite |
|-----|-----------|
| `heimatplatz://property/{guid}` | PropertyDetailPage |
| `heimatplatz://foreclosure/{guid}` | ForeclosureDetailPage |

## Services

### IDeepLinkService

```csharp
public interface IDeepLinkService
{
    Task<bool> HandleDeepLinkAsync(Uri uri);
    bool CanHandleUri(Uri uri);
}
```

## Verwendung

1. In `App.xaml.cs` den `OnActivated`-Handler implementieren
2. Bei Protocol-Aktivierung `IDeepLinkService.HandleDeepLinkAsync()` aufrufen

## Plattform-Konfiguration

### Android
IntentFilter auf MainActivity mit `DataScheme = "heimatplatz"`

### iOS
CFBundleURLTypes in Info.plist mit Schema `heimatplatz`
