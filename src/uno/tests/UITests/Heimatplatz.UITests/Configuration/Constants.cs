namespace Heimatplatz.UITests.Configuration;

/// <summary>
/// Unterstuetzte Test-Plattformen.
/// </summary>
public enum Platform
{
    /// <summary>
    /// WebAssembly im Browser (Chrome).
    /// </summary>
    Browser,

    /// <summary>
    /// Android Emulator oder Geraet.
    /// </summary>
    Android,

    /// <summary>
    /// iOS Simulator oder Geraet.
    /// </summary>
    iOS
}

/// <summary>
/// Platform-Konstanten fuer UI-Tests.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Aktuelle Test-Plattform. Kann via Environment-Variable UITEST_PLATFORM ueberschrieben werden.
    /// </summary>
    public static Platform CurrentPlatform =>
        Environment.GetEnvironmentVariable("UITEST_PLATFORM") switch
        {
            "Browser" => Platform.Browser,
            "Android" => Platform.Android,
            "iOS" => Platform.iOS,
            _ => Platform.Browser // Default fuer lokale Entwicklung
        };

    /// <summary>
    /// WebAssembly App URI. Kann via Environment-Variable UITEST_WASM_URI ueberschrieben werden.
    /// </summary>
    public static string WebAssemblyDefaultUri =>
        Environment.GetEnvironmentVariable("UITEST_WASM_URI")
        ?? "http://localhost:5000";

    /// <summary>
    /// Android App Package Name. Kann via Environment-Variable UITEST_ANDROID_PACKAGE ueberschrieben werden.
    /// </summary>
    public static string AndroidAppPackage =>
        Environment.GetEnvironmentVariable("UITEST_ANDROID_PACKAGE")
        ?? "com.companyname.heimatplatz";

    /// <summary>
    /// iOS App Bundle ID. Kann via Environment-Variable UITEST_IOS_BUNDLE ueberschrieben werden.
    /// </summary>
    public static string iOSAppBundleId =>
        Environment.GetEnvironmentVariable("UITEST_IOS_BUNDLE")
        ?? "com.companyname.heimatplatz";

    /// <summary>
    /// Android APK Pfad fuer CI/CD. Kann via Environment-Variable UITEST_ANDROID_APK ueberschrieben werden.
    /// </summary>
    public static string? AndroidApkPath =>
        Environment.GetEnvironmentVariable("UITEST_ANDROID_APK");

    /// <summary>
    /// iOS App Pfad fuer CI/CD. Kann via Environment-Variable UITEST_IOS_APP ueberschrieben werden.
    /// </summary>
    public static string? iOSAppPath =>
        Environment.GetEnvironmentVariable("UITEST_IOS_APP");

    /// <summary>
    /// Standard-Timeout fuer Element-Waits.
    /// </summary>
    public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Kurzer Timeout fuer schnelle Operationen.
    /// </summary>
    public static TimeSpan ShortTimeout => TimeSpan.FromSeconds(5);

    /// <summary>
    /// Langer Timeout fuer langsame Operationen (z.B. Navigation, API-Calls).
    /// </summary>
    public static TimeSpan LongTimeout => TimeSpan.FromSeconds(60);
}
