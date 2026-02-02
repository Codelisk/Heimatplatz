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

        // Explicit restore for the specific framework to ensure project.assets.json is correct
        // Use root nuget.config to avoid conflicts with submodule configs
        context.Information("Restoring packages for net10.0-android...");
        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        restoreSettings.MSBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        restoreSettings.MSBuildSettings.Properties["TargetFramework"] = new[] { "net10.0-android" };
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "android");
        Directory.CreateDirectory(outputDir);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        // Use APK format for apps not enrolled in Play App Signing
        msBuildSettings.Properties["AndroidPackageFormat"] = new[] { "apk" };

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
