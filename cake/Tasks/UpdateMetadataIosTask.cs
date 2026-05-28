using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("UpdateMetadataIos")]
public sealed class UpdateMetadataIosTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS metadata update can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== Update iOS Store Metadata ===");

        if (string.IsNullOrEmpty(context.AppStoreConnectApiKeyId))
        {
            throw new InvalidOperationException(
                "ASC_KEY_ID not configured. Set iOS:AppStoreConnectApiKeyId in appsettings.json or ASC_KEY_ID env var.");
        }

        context.Information($"Running Fastlane from: {context.FastlaneDirectory}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "ios update_metadata",
            WorkingDirectory = context.FastlaneDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

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
            throw new InvalidOperationException($"Fastlane failed with exit code {process.ExitCode}");
        }

        context.Information("iOS App Store metadata uploaded successfully!");
    }
}
