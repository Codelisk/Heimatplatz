using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Erzeugt die Play-Store-Grafiken (icon.png 512x512, featureGraphic.png 1024x500)
/// frisch aus den Markenassets (logo-mark.svg + Schibsted-Grotesk-Fonts) via
/// Headless-Chrome - Vorlagen in cake/store-art/. Laeuft vor dem Listing-Upload,
/// damit die Store-Grafiken nie wieder vom Branding driften (das alte Logo lag
/// monatelang unbemerkt als statisches PNG im metadata-Ordner).
/// Ohne auffindbares Chrome: Warnung + eingecheckte PNGs bleiben als Fallback.
/// </summary>
[TaskName("GenerateStoreArt")]
public sealed class StoreArtTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => RunCore(context);

    public static void RunCore(BuildContext context)
    {
        context.Information("=== Generate Store Art (aus logo-mark.svg) ===");

        var chrome = FindChrome();
        if (chrome is null)
        {
            context.Warning("Kein Chrome/Chromium gefunden (CHROME_PATH setzen?) - " +
                            "eingecheckte Store-Grafiken bleiben unveraendert.");
            return;
        }

        var artDir = Path.Combine(context.BuildDirectory, "store-art");
        var imagesDir = Path.Combine(context.FastlaneDirectory, "metadata", "android", "de-DE", "images");
        Directory.CreateDirectory(imagesDir);

        Render(context, chrome, Path.Combine(artDir, "icon.html"), 512, 512,
            Path.Combine(imagesDir, "icon.png"));
        Render(context, chrome, Path.Combine(artDir, "feature-graphic.html"), 1024, 500,
            Path.Combine(imagesDir, "featureGraphic.png"));

        context.Information("Store-Grafiken aktualisiert.");
    }

    private static void Render(
        BuildContext context, string chrome, string htmlPath, int width, int height, string outputPath)
    {
        var htmlUri = new Uri(htmlPath).AbsoluteUri;
        AstroWeb.RunProcess(
            context,
            chrome,
            $"--headless=new --disable-gpu --force-device-scale-factor=1 " +
            $"--window-size={width},{height} --screenshot=\"{outputPath}\" \"{htmlUri}\"",
            Path.GetDirectoryName(htmlPath)!,
            label: $"chrome ({Path.GetFileName(outputPath)})",
            timeoutMinutes: 2);

        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException($"Chrome hat {outputPath} nicht erzeugt.");
        }
        context.Information($"{Path.GetFileName(outputPath)} -> {width}x{height} erzeugt.");
    }

    private static string? FindChrome()
    {
        var envPath = Environment.GetEnvironmentVariable("CHROME_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        string[] windowsCandidates =
        [
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
        ];
        foreach (var candidate in windowsCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        // Linux/macOS (CI): auf dem PATH suchen
        string[] pathCandidates = ["google-chrome", "chromium-browser", "chromium"];
        foreach (var candidate in pathCandidates)
        {
            try
            {
                using var which = Process.Start(new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = candidate,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                which!.WaitForExit(5_000);
                if (which.ExitCode == 0)
                    return candidate;
            }
            catch
            {
                // weiter probieren
            }
        }

        return null;
    }
}
