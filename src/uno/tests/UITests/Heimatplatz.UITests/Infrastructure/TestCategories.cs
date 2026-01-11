namespace Heimatplatz.UITests.Infrastructure;

/// <summary>
/// NUnit Test-Kategorien fuer Plattform-Filter in CI/CD.
/// Verwendung: [Category(TestCategories.WebAssembly)]
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Tests die auf WebAssembly laufen.
    /// </summary>
    public const string WebAssembly = "WebAssembly";

    /// <summary>
    /// Tests die auf Android laufen.
    /// </summary>
    public const string Android = "Android";

    /// <summary>
    /// Tests die auf iOS laufen.
    /// </summary>
    public const string iOS = "iOS";

    /// <summary>
    /// Smoke-Tests fuer schnelle Validierung.
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Regressionstests.
    /// </summary>
    public const string Regression = "Regression";

    /// <summary>
    /// Tests fuer kritische Pfade.
    /// </summary>
    public const string Critical = "Critical";

    /// <summary>
    /// Langsame Tests die nicht bei jedem Build laufen sollten.
    /// </summary>
    public const string Slow = "Slow";

    /// <summary>
    /// Core-Funktionalitaet Tests (Shell, MainPage, grundlegende Navigation).
    /// </summary>
    public const string Core = "Core";

    /// <summary>
    /// Navigation-bezogene Tests.
    /// </summary>
    public const string Navigation = "Navigation";

    /// <summary>
    /// Authentifizierung-bezogene Tests.
    /// </summary>
    public const string Auth = "Auth";

    /// <summary>
    /// Feature-spezifische Tests.
    /// </summary>
    public const string Feature = "Feature";
}
