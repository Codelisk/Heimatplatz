using Uno.UITest;

namespace Heimatplatz.UITests.Helpers;

/// <summary>
/// Helper fuer Screenshot-Capture und Test-Dokumentation.
/// </summary>
public class ScreenshotHelper
{
    private readonly IApp _app;
    private int _screenshotCounter;

    public ScreenshotHelper(IApp app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _screenshotCounter = 0;
    }

    /// <summary>
    /// Erstellt einen Screenshot mit dem angegebenen Namen.
    /// </summary>
    public void TakeScreenshot(string name)
    {
        var sanitizedName = SanitizeFileName(name);
        _app.Screenshot(sanitizedName);
    }

    /// <summary>
    /// Erstellt einen nummerierten Screenshot.
    /// </summary>
    public void TakeNumberedScreenshot(string prefix = "Step")
    {
        _screenshotCounter++;
        var name = $"{prefix}_{_screenshotCounter:D3}";
        TakeScreenshot(name);
    }

    /// <summary>
    /// Erstellt einen Screenshot mit Testname und Schritt.
    /// </summary>
    public void TakeStepScreenshot(string testName, string stepDescription)
    {
        _screenshotCounter++;
        var name = $"{testName}_{_screenshotCounter:D3}_{stepDescription}";
        TakeScreenshot(name);
    }

    /// <summary>
    /// Erstellt einen Screenshot bei Fehler.
    /// </summary>
    public void TakeErrorScreenshot(string testName, string errorMessage)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var name = $"ERROR_{testName}_{timestamp}";
        TakeScreenshot(name);

        // Log error message for debugging
        Console.WriteLine($"[ERROR] {testName}: {errorMessage}");
    }

    /// <summary>
    /// Setzt den Screenshot-Counter zurueck.
    /// </summary>
    public void ResetCounter()
    {
        _screenshotCounter = 0;
    }

    /// <summary>
    /// Erstellt einen Screenshot vor und nach einer Aktion.
    /// </summary>
    public void CaptureBeforeAndAfter(string actionName, Action action)
    {
        TakeScreenshot($"Before_{actionName}");

        try
        {
            action();
            TakeScreenshot($"After_{actionName}");
        }
        catch (Exception ex)
        {
            TakeErrorScreenshot(actionName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Bereinigt den Dateinamen von ungültigen Zeichen.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Begrenze die Länge
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized;
    }
}
