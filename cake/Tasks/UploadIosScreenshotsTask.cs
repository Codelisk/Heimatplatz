using System.Diagnostics;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Laedt die von <see cref="IosScreenshotsTask"/> erzeugten Screenshots
/// (artifacts/screenshots/ios/&lt;locale&gt;/*.png) via Fastlane deliver in
/// App Store Connect hoch - nur Screenshots, keine Metadaten, kein Binary.
/// </summary>
[TaskName("UploadIosScreenshots")]
public sealed class UploadIosScreenshotsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS screenshot upload can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== Upload iOS App Store Screenshots ===");

        if (string.IsNullOrEmpty(context.AppStoreConnectApiKeyId))
        {
            throw new InvalidOperationException(
                "ASC_KEY_ID not configured. Set iOS:AppStoreConnectApiKeyId in appsettings.json or ASC_KEY_ID env var.");
        }

        var screenshotsDir = Path.Combine(context.ProjectDirectory, "artifacts", "screenshots", "ios");
        if (!Directory.Exists(screenshotsDir) ||
            Directory.GetFiles(screenshotsDir, "*.png", SearchOption.AllDirectories).Length == 0)
        {
            throw new InvalidOperationException(
                $"No screenshots found under {screenshotsDir}. Run the IosScreenshots task first.");
        }

        // Screenshots haengen an einer App-Store-Version - deliver braucht eine editierbare
        // Version und legt sie mit app_version bei Bedarf an ("Prepare for Submission")
        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var displayVersion = doc.Descendants(ns + "ApplicationDisplayVersion").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("ApplicationDisplayVersion not found in csproj");

        context.Information($"Uploading screenshots from: {screenshotsDir}");
        context.Information($"Target App Store version: {displayVersion}");
        context.Information($"Running Fastlane from: {context.FastlaneDirectory}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "ios upload_screenshots",
            WorkingDirectory = context.FastlaneDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_ID"] = context.AppStoreConnectApiKeyId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_ISSUER_ID"] = context.AppStoreConnectIssuerId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_FILEPATH"] = context.AppStoreConnectKeyPath;
        processInfo.Environment["DELIVER_SCREENSHOTS_PATH"] = screenshotsDir;
        processInfo.Environment["DELIVER_APP_VERSION"] = displayVersion;

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Fastlane process");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        context.Information(output);
        if (!string.IsNullOrEmpty(error))
        {
            context.Warning(error);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Fastlane failed with exit code {process.ExitCode}");
        }

        context.Information("iOS App Store screenshots uploaded successfully!");
    }
}
