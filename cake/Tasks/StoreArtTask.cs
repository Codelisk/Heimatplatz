using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Erzeugt ALLE abgeleiteten Brand-Grafiken frisch aus den Markenassets
/// (logo-mark.svg / logo-mono.svg + Schibsted-Grotesk-Fonts) via Headless-Chrome -
/// Vorlagen in cake/store-art/:
///  - Play Store: icon.png 512, featureGraphic.png 1024x500 (fastlane metadata)
///  - iOS 18 AppIcon: light/dark/tinted 1024 (Platforms/iOS/.../AppIcon.appiconset)
///  - iOS SplashLogo: 256/512/768 (SplashLogo.imageset, LaunchScreen.storyboard)
///  - Web PWA: icon-192/512 + maskable-Varianten (src/web/public/icons)
///  - Wordmark-Lockup: light/dark 1200x320 (store-assets/brand)
/// Laeuft vor dem Listing-Upload, damit nichts mehr vom Branding driftet (das alte
/// Logo lag monatelang unbemerkt als statisches PNG im metadata-Ordner).
/// Ohne auffindbares Chrome: Warnung + eingecheckte PNGs bleiben als Fallback.
/// </summary>
[TaskName("GenerateStoreArt")]
public sealed class StoreArtTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => RunCore(context);

    public static void RunCore(BuildContext context)
    {
        context.Information("=== Generate Store Art (aus logo-mark.svg / logo-mono.svg) ===");

        var chrome = FindChrome();
        if (chrome is null)
        {
            context.Warning("Kein Chrome/Chromium gefunden (CHROME_PATH setzen?) - " +
                            "eingecheckte Store-Grafiken bleiben unveraendert.");
            return;
        }

        var artDir = Path.Combine(context.BuildDirectory, "store-art");

        // Play Store (fastlane metadata)
        var imagesDir = Path.Combine(context.FastlaneDirectory, "metadata", "android", "de-DE", "images");
        Directory.CreateDirectory(imagesDir);
        Render(context, chrome, Path.Combine(artDir, "icon.html"), 512, 512,
            Path.Combine(imagesDir, "icon.png"));
        Render(context, chrome, Path.Combine(artDir, "feature-graphic.html"), 1024, 500,
            Path.Combine(imagesDir, "featureGraphic.png"));

        // iOS 18 AppIcon (manuelles Asset-Catalog, siehe csproj/Info.plist der MAUI-App)
        var appIconSet = Path.Combine(context.ProjectDirectory,
            "src", "maui", "src", "Heimatplatz.Maui", "Platforms", "iOS", "Resources",
            "Assets.xcassets", "AppIcon.appiconset");
        Directory.CreateDirectory(appIconSet);
        Render(context, chrome, Path.Combine(artDir, "ios-icon-light.html"), 1024, 1024,
            Path.Combine(appIconSet, "icon-1024-light.png"));
        Render(context, chrome, Path.Combine(artDir, "ios-icon-dark.html"), 1024, 1024,
            Path.Combine(appIconSet, "icon-1024-dark.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "ios-icon-tinted.html"), 1024, 1024,
            Path.Combine(appIconSet, "icon-1024-tinted.png"), transparent: true);

        // iOS SplashLogo (LaunchScreen.storyboard, 1x/2x/3x)
        var splashSet = Path.Combine(context.ProjectDirectory,
            "src", "maui", "src", "Heimatplatz.Maui", "Platforms", "iOS", "Resources",
            "Assets.xcassets", "SplashLogo.imageset");
        Directory.CreateDirectory(splashSet);
        Render(context, chrome, Path.Combine(artDir, "badge-transparent.html"), 256, 256,
            Path.Combine(splashSet, "splash-logo.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "badge-transparent.html"), 512, 512,
            Path.Combine(splashSet, "splash-logo@2x.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "badge-transparent.html"), 768, 768,
            Path.Combine(splashSet, "splash-logo@3x.png"), transparent: true);

        // Web PWA-Icons (manifest.webmanifest)
        var webIcons = Path.Combine(context.ProjectDirectory, "src", "web", "public", "icons");
        Directory.CreateDirectory(webIcons);
        Render(context, chrome, Path.Combine(artDir, "badge-transparent.html"), 192, 192,
            Path.Combine(webIcons, "icon-192.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "badge-transparent.html"), 512, 512,
            Path.Combine(webIcons, "icon-512.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "pwa-maskable.html"), 192, 192,
            Path.Combine(webIcons, "icon-maskable-192.png"));
        Render(context, chrome, Path.Combine(artDir, "pwa-maskable.html"), 512, 512,
            Path.Combine(webIcons, "icon-maskable-512.png"));

        // Wordmark-Lockup (E-Mails, Presse, Social)
        var brandDir = Path.Combine(context.ProjectDirectory, "store-assets", "brand");
        Directory.CreateDirectory(brandDir);
        Render(context, chrome, Path.Combine(artDir, "wordmark-light.html"), 1200, 320,
            Path.Combine(brandDir, "wordmark-light.png"), transparent: true);
        Render(context, chrome, Path.Combine(artDir, "wordmark-dark.html"), 1200, 320,
            Path.Combine(brandDir, "wordmark-dark.png"), transparent: true);

        context.Information("Store-/Brand-Grafiken aktualisiert.");
    }

    private static void Render(
        BuildContext context, string chrome, string htmlPath, int width, int height, string outputPath,
        bool transparent = false)
    {
        var htmlUri = new Uri(htmlPath).AbsoluteUri;
        // 00000000 = voll transparenter Default-Background, sonst malt Chrome Weiss unter die Seite
        var background = transparent ? "--default-background-color=00000000 " : "";
        AstroWeb.RunProcess(
            context,
            chrome,
            $"--headless=new --disable-gpu --force-device-scale-factor=1 {background}" +
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
