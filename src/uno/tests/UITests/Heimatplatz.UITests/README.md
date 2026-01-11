# Heimatplatz.UITests

Shared UI-Test-Infrastruktur fuer das Heimatplatz Projekt basierend auf **Uno.UITest**.

## Uebersicht

Dieses Projekt stellt die gemeinsame Test-Infrastruktur fuer alle UI-Tests bereit:

- **Configuration**: Plattform-Konfiguration und App-Initialisierung
- **Infrastructure**: Basis-Testklassen und Helper
- **PageObjects**: Page Object Pattern Basis-Implementierung
- **Helpers**: Wait-, Screenshot- und TestData-Helper

## Projektstruktur

```
Heimatplatz.UITests/
|
+-- Configuration/
|   +-- Constants.cs              # Plattform-Konstanten und Timeouts
|   +-- AppInitializer.cs         # App-Start fuer verschiedene Plattformen
|
+-- Infrastructure/
|   +-- BaseTestFixture.cs        # Basis-Testklasse (erben!)
|   +-- TestCategories.cs         # NUnit Kategorien
|   +-- PlatformHelper.cs         # Plattform-spezifische Hilfsmethoden
|
+-- PageObjects/
|   +-- BasePage.cs               # Abstrakte Basis fuer Page Objects
|   +-- Extensions/
|       +-- AppExtensions.cs      # IApp Erweiterungsmethoden
|
+-- Helpers/
    +-- WaitHelper.cs             # Explizite Waits
    +-- ScreenshotHelper.cs       # Screenshot-Capture
    +-- TestDataHelper.cs         # Testdaten-Generierung
```

## Verwendung

### 1. Test-Klasse erstellen

```csharp
using Heimatplatz.UITests.Infrastructure;

namespace Heimatplatz.Features.Auth.UITests.Tests;

[TestFixture]
[Category(TestCategories.WebAssembly)]
public class LoginTests : BaseTestFixture
{
    [Test]
    public void Login_WithValidCredentials_Succeeds()
    {
        // Arrange
        var loginPage = new LoginPage(App);

        // Act
        loginPage.EnterUsername("testuser@example.com");
        loginPage.EnterPassword("Test123!");
        loginPage.TapLogin();

        // Assert
        WaitForElement("MainPage_WelcomeText");
    }
}
```

### 2. Page Object erstellen

```csharp
using Heimatplatz.UITests.PageObjects;

namespace Heimatplatz.Features.Auth.UITests.PageObjects;

public class LoginPage : BasePage
{
    private const string UsernameField = "LoginPage_Username";
    private const string PasswordField = "LoginPage_Password";
    private const string LoginButton = "LoginPage_LoginButton";

    public LoginPage(IApp app) : base(app) { }

    public LoginPage EnterUsername(string username)
    {
        EnterText(UsernameField, username);
        return this;
    }

    public LoginPage EnterPassword(string password)
    {
        EnterText(PasswordField, password);
        return this;
    }

    public void TapLogin() => Tap(LoginButton);
}
```

### 3. XAML AutomationIds setzen

```xml
<TextBox AutomationProperties.AutomationId="LoginPage_Username" />
<PasswordBox AutomationProperties.AutomationId="LoginPage_Password" />
<Button AutomationProperties.AutomationId="LoginPage_LoginButton" />
```

## Konfiguration

### Environment-Variablen

| Variable | Beschreibung | Default |
|----------|--------------|---------|
| `UITEST_PLATFORM` | Test-Plattform (`Browser`, `Android`, `iOS`) | `Browser` |
| `UITEST_WASM_URI` | WebAssembly App URL | `http://localhost:5000` |
| `UITEST_ANDROID_PACKAGE` | Android Package Name | `com.companyname.heimatplatz` |
| `UITEST_ANDROID_APK` | Pfad zur APK (CI/CD) | - |
| `UITEST_IOS_BUNDLE` | iOS Bundle ID | `com.companyname.heimatplatz` |
| `UITEST_IOS_APP` | Pfad zur iOS App (CI/CD) | - |

### Lokale Entwicklung

```powershell
# WebAssembly Tests
$env:UITEST_PLATFORM = "Browser"
$env:UITEST_WASM_URI = "http://localhost:5000"
dotnet test

# Android Tests
$env:UITEST_PLATFORM = "Android"
dotnet test
```

## Test-Kategorien

```csharp
[Category(TestCategories.WebAssembly)]  // Nur WebAssembly
[Category(TestCategories.Android)]      // Nur Android
[Category(TestCategories.iOS)]          // Nur iOS
[Category(TestCategories.Smoke)]        // Schnelle Validierung
[Category(TestCategories.Critical)]     // Kritische Pfade
[Category(TestCategories.Slow)]         // Langsame Tests
```

### Tests filtern

```powershell
# Nur WebAssembly Tests
dotnet test --filter "Category=WebAssembly"

# Smoke Tests
dotnet test --filter "Category=Smoke"

# Ohne langsame Tests
dotnet test --filter "Category!=Slow"
```

## CI/CD Integration

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run UI Tests'
  inputs:
    command: 'test'
    projects: 'src/uno/tests/**/*.UITests.csproj'
    arguments: '--filter "Category=WebAssembly"'
  env:
    UITEST_PLATFORM: 'Browser'
    UITEST_WASM_URI: '$(WebAppUrl)'
```

### GitHub Actions

```yaml
- name: Run UI Tests
  run: dotnet test src/uno/tests/**/*.UITests.csproj --filter "Category=WebAssembly"
  env:
    UITEST_PLATFORM: Browser
    UITEST_WASM_URI: http://localhost:5000
```

## Best Practices

1. **Page Object Pattern**: Jede Seite bekommt ein Page Object
2. **AutomationIds**: Namensschema `{PageName}_{Purpose}`
3. **Explizite Waits**: `WaitForElement()` statt `Thread.Sleep()`
4. **Test-Kategorien**: Fuer Plattform-Filterung in CI/CD
5. **Screenshots**: Automatisch bei Fehlern, manuell fuer Dokumentation

## Abhaengigkeiten

- `Uno.UITest` - UI-Test Framework
- `NUnit` 4.x - Test Framework
- `FluentAssertions` - Assertions
- `Microsoft.NET.Test.Sdk` - Test SDK
