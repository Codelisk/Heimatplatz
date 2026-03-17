using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildIosDev")]
public sealed class BuildIosDevTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS build task can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== Build iOS Dev Task (Device) ===");
        context.Information($"Building project: {context.CsprojPath}");

        // Sync development certificates using Fastlane Match
        if (!string.IsNullOrEmpty(context.MatchGitUrl))
        {
            context.Information("Syncing development certificates with Fastlane Match...");
            RunFastlaneMatch(context);
        }
        else
        {
            context.Warning("MATCH_GIT_URL not configured. Using local signing identity.");
        }

        Environment.SetEnvironmentVariable("UNO_SINGLE_TARGET", "ios");

        context.Information("Restoring packages for net10.0-ios...");
        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        restoreSettings.MSBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        restoreSettings.MSBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "ios" };
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "ios-dev");
        Directory.CreateDirectory(outputDir);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        msBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "ios" };
        msBuildSettings.Properties["RuntimeIdentifier"] = new[] { "ios-arm64" };

        if (!string.IsNullOrEmpty(context.IosTeamId))
        {
            msBuildSettings.Properties["CodesignKey"] = new[] { "Apple Development" };
        }

        // Use development entitlements for device builds
        var devEntitlements = Path.Combine(context.ProjectDirectory,
            "src", "uno", "src", "Heimatplatz.App", "Platforms", "iOS", "Entitlements.Development.plist");
        if (File.Exists(devEntitlements))
        {
            msBuildSettings.Properties["CodesignEntitlements"] = new[] { devEntitlements };
        }

        var settings = new DotNetPublishSettings
        {
            Configuration = "Debug",
            Framework = "net10.0-ios",
            OutputDirectory = outputDir,
            MSBuildSettings = msBuildSettings
        };

        context.DotNetPublish(context.CsprojPath, settings);

        context.Information($"iOS dev build completed. Output: {outputDir}");
    }

    private void RunFastlaneMatch(BuildContext context)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "match development --readonly",
            WorkingDirectory = context.FastlaneDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.Environment["MATCH_GIT_URL"] = context.MatchGitUrl;
        processInfo.Environment["MATCH_PASSWORD"] = context.MatchPassword;
        processInfo.Environment["MATCH_APP_IDENTIFIER"] = context.IosBundleId;
        processInfo.Environment["APPLE_TEAM_ID"] = context.IosTeamId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_ID"] = context.AppStoreConnectApiKeyId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_ISSUER_ID"] = context.AppStoreConnectIssuerId;
        processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_FILEPATH"] = context.AppStoreConnectKeyPath;

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Fastlane Match process");
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
            throw new InvalidOperationException($"Fastlane Match failed with exit code {process.ExitCode}");
        }
    }
}
