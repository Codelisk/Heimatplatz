using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployIos")]
[IsDependentOn(typeof(ComplianceCheckTask))]
[IsDependentOn(typeof(BuildIosTask))]
public sealed class DeployIosTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS deploy task can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy iOS Task (TestFlight) ===");

        var artifactsDir = Path.Combine(context.ProjectDirectory, "artifacts", "ios");
        var ipaFiles = Directory.GetFiles(artifactsDir, "*.ipa", SearchOption.AllDirectories);

        if (ipaFiles.Length == 0)
        {
            throw new FileNotFoundException($"No IPA file found in {artifactsDir}");
        }

        var ipaPath = ipaFiles.First();
        context.Information($"Found IPA: {ipaPath}");

        if (string.IsNullOrEmpty(context.AppStoreConnectApiKeyId))
        {
            context.Warning("ASC_KEY_ID not configured. Skipping TestFlight upload.");
            context.Information("To enable upload, configure App Store Connect API credentials.");
            return;
        }

        context.Information($"Running Fastlane from: {context.FastlaneDirectory}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "ios beta",
            WorkingDirectory = context.FastlaneDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.Environment["PILOT_IPA"] = ipaPath;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_ID"] = context.AppStoreConnectApiKeyId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_ISSUER_ID"] = context.AppStoreConnectIssuerId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_FILEPATH"] = context.AppStoreConnectKeyPath;

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
            var combinedOutput = output + error;

            if (combinedOutput.Contains("agreement", StringComparison.OrdinalIgnoreCase) &&
                (combinedOutput.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                 combinedOutput.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
                 combinedOutput.Contains("in-effect", StringComparison.OrdinalIgnoreCase)))
            {
                context.Error("╔══════════════════════════════════════════════════════════════════════╗");
                context.Error("║  APP STORE CONNECT - AGREEMENT REQUIRED                              ║");
                context.Error("╠══════════════════════════════════════════════════════════════════════╣");
                context.Error("║  A required agreement is missing or has expired.                     ║");
                context.Error("║                                                                      ║");
                context.Error("║  Please visit: https://appstoreconnect.apple.com/agreements          ║");
                context.Error("║  Accept all pending agreements, then re-run the build.               ║");
                context.Error("╚══════════════════════════════════════════════════════════════════════╝");
            }

            throw new InvalidOperationException($"Fastlane failed with exit code {process.ExitCode}");
        }

        context.Information("iOS deployment to TestFlight completed!");
    }
}
