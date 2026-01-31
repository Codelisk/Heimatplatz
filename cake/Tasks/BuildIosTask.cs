using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildIos")]
[IsDependentOn(typeof(VersionBumpTask))]
public sealed class BuildIosTask : FrostingTask<BuildContext>
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
        context.Information("=== Build iOS Task ===");
        context.Information($"Building project: {context.CsprojPath}");

        // Sync certificates using Fastlane Match
        if (!string.IsNullOrEmpty(context.MatchGitUrl))
        {
            context.Information("Syncing certificates with Fastlane Match...");
            RunFastlaneMatch(context);
        }
        else
        {
            context.Warning("MATCH_GIT_URL not configured. Using local signing identity.");
        }

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "ios");
        Directory.CreateDirectory(outputDir);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        msBuildSettings.Properties["RuntimeIdentifier"] = new[] { "ios-arm64" };
        msBuildSettings.Properties["ArchiveOnBuild"] = new[] { "true" };

        if (!string.IsNullOrEmpty(context.IosTeamId))
        {
            msBuildSettings.Properties["CodesignKey"] = new[] { "Apple Distribution" };
        }

        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            Framework = "net10.0-ios",
            OutputDirectory = outputDir,
            MSBuildSettings = msBuildSettings
        };

        context.DotNetPublish(context.CsprojPath, settings);

        context.Information($"iOS build completed. Output: {outputDir}");
    }

    private void RunFastlaneMatch(BuildContext context)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "match appstore --readonly",
            WorkingDirectory = context.FastlaneDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.Environment["MATCH_GIT_URL"] = context.MatchGitUrl;
        processInfo.Environment["MATCH_PASSWORD"] = context.MatchPassword;
        processInfo.Environment["MATCH_APP_IDENTIFIER"] = context.IosBundleId;

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
