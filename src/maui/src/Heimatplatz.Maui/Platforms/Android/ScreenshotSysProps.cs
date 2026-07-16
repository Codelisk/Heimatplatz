using System.Diagnostics;

namespace Heimatplatz.Maui.Platforms.Android;

/// <summary>
/// Android-Gegenstueck zur SIMCTL_CHILD_*-Mechanik auf iOS: der Cake-Task
/// "AndroidScreenshots" setzt vor jedem App-Start "debug.heimatplatz.*"-System-Properties
/// via "adb shell setprop" (debug.* ist ohne Root erlaubt). Diese Klasse liest sie beim
/// Prozessstart und exportiert sie als Umgebungsvariablen, damit der geteilte
/// ScreenshotMode und der HEIMATPLATZ_API_URL-Override unveraendert funktionieren.
/// Sicherheitsgurt: laeuft ausschliesslich im Emulator - auf echten Geraeten (Store-Build)
/// werden die Properties nie gelesen, der Modus bleibt tot.
/// </summary>
public static class ScreenshotSysProps
{
    private static readonly (string Prop, string EnvVar)[] Map =
    [
        ("debug.heimatplatz.screenshot", "SCREENSHOT_MODE"),
        ("debug.heimatplatz.route", "SCREENSHOT_ROUTE"),
        ("debug.heimatplatz.email", "SCREENSHOT_LOGIN_EMAIL"),
        ("debug.heimatplatz.password", "SCREENSHOT_LOGIN_PASSWORD"),
        ("debug.heimatplatz.apiurl", "HEIMATPLATZ_API_URL"),
        ("debug.heimatplatz.navdelay", "SCREENSHOT_NAV_DELAY_MS")
    ];

    /// <summary>Muss vor MauiProgram.CreateMauiApp laufen (API-URL wird dort fixiert).</summary>
    public static void TryImport()
    {
        if (!IsEmulator())
            return;

        foreach (var (prop, envVar) in Map)
        {
            var value = GetProp(prop);
            if (!string.IsNullOrWhiteSpace(value))
            {
                Environment.SetEnvironmentVariable(envVar, value);
            }
        }
    }

    private static bool IsEmulator()
    {
        var hardware = global::Android.OS.Build.Hardware ?? string.Empty;
        var fingerprint = global::Android.OS.Build.Fingerprint ?? string.Empty;
        var model = global::Android.OS.Build.Model ?? string.Empty;

        return hardware.StartsWith("ranchu", StringComparison.OrdinalIgnoreCase)
            || hardware.StartsWith("goldfish", StringComparison.OrdinalIgnoreCase)
            || hardware.StartsWith("cutf", StringComparison.OrdinalIgnoreCase)
            || fingerprint.Contains("generic", StringComparison.OrdinalIgnoreCase)
            || fingerprint.Contains("emulator", StringComparison.OrdinalIgnoreCase)
            || model.Contains("sdk_gphone", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Liest eine System-Property ueber /system/bin/getprop (SystemProperties ist Hidden-API).</summary>
    private static string? GetProp(string name)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/system/bin/getprop",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            processInfo.ArgumentList.Add(name);

            using var process = Process.Start(processInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);
            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScreenshotMode] getprop {name} failed: {ex.Message}");
            return null;
        }
    }
}
