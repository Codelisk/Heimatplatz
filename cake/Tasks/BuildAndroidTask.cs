using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildAndroid")]
[IsDependentOn(typeof(VersionBumpTask))]
public sealed class BuildAndroidTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Build Android Task ===");
        context.Information($"Building project: {context.CsprojPath}");

        // Set UNO_SINGLE_TARGET to android so UnoFrameworks resolves to net10.0-android only
        // This ensures project.assets.json contains the correct target framework
        // Note: Using "false" (all platforms) doesn't work on Linux as iOS workloads are unavailable
        Environment.SetEnvironmentVariable("UNO_SINGLE_TARGET", "android");

        // Explicit restore for android framework to ensure project.assets.json is correct
        // Use root nuget.config to avoid conflicts with submodule configs
        context.Information("Restoring packages for net10.0-android...");
        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        restoreSettings.MSBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        restoreSettings.MSBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "android" };
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "android");
        Directory.CreateDirectory(outputDir);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        // Ensure UNO_SINGLE_TARGET is set for publish as well (in case it triggers implicit restore)
        msBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "android" };
        // Use AAB format for Google Play Store
        msBuildSettings.Properties["AndroidPackageFormat"] = new[] { "aab" };

        // Add signing properties if keystore is configured
        if (!string.IsNullOrEmpty(context.AndroidKeystorePath) && File.Exists(context.AndroidKeystorePath))
        {
            context.Information("Using keystore for signing");
            msBuildSettings.Properties["AndroidKeyStore"] = new[] { "true" };
            msBuildSettings.Properties["AndroidSigningKeyStore"] = new[] { context.AndroidKeystorePath };
            msBuildSettings.Properties["AndroidSigningKeyAlias"] = new[] { context.AndroidKeyAlias };
            msBuildSettings.Properties["AndroidSigningKeyPass"] = new[] { context.AndroidKeyPassword };
            msBuildSettings.Properties["AndroidSigningStorePass"] = new[] { context.AndroidKeystorePassword };
        }
        else
        {
            context.Warning("No keystore configured - building unsigned APK");
        }

        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            Framework = "net10.0-android",
            OutputDirectory = outputDir,
            MSBuildSettings = msBuildSettings
        };

        context.DotNetPublish(context.CsprojPath, settings);

        context.Information($"Android build completed. Output: {outputDir}");
    }
}
