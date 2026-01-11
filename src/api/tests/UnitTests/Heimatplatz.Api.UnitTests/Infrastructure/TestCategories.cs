namespace Heimatplatz.Api.UnitTests.Infrastructure;

/// <summary>
/// Konstanten fuer Test-Kategorien.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Unit-Tests - schnelle, isolierte Tests.
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Schnelle Tests (unter 100ms).
    /// </summary>
    public const string Fast = "Fast";

    /// <summary>
    /// Langsame Tests (ueber 100ms).
    /// </summary>
    public const string Slow = "Slow";

    /// <summary>
    /// Smoke-Tests - kritische Pfade.
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Core-Modul Tests.
    /// </summary>
    public const string Core = "Core";

    /// <summary>
    /// Auth-Feature Tests.
    /// </summary>
    public const string Auth = "Auth";

    /// <summary>
    /// Handler Tests.
    /// </summary>
    public const string Handler = "Handler";

    /// <summary>
    /// Data-Layer Tests.
    /// </summary>
    public const string Data = "Data";
}
