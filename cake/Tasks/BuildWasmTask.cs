using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildWasm")]
public sealed class BuildWasmTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Build WebAssembly Task ===");
        context.Information($"Building project: {context.CsprojPath}");

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "wasm");
        Directory.CreateDirectory(outputDir);

        // Replace API_BASE_URL in appsettings.json if configured
        if (!string.IsNullOrEmpty(context.ApiBaseUrl))
        {
            var appSettingsPath = Path.Combine(
                Path.GetDirectoryName(context.CsprojPath)!,
                "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                var content = File.ReadAllText(appSettingsPath);
                if (content.Contains("__API_BASE_URL__"))
                {
                    content = content.Replace("__API_BASE_URL__", context.ApiBaseUrl);
                    File.WriteAllText(appSettingsPath, content);
                    context.Information($"Replaced API_BASE_URL with: {context.ApiBaseUrl}");
                }
            }
        }

        // Set UNO_SINGLE_TARGET to false so UnoFrameworks includes all platforms
        // This ensures project.assets.json contains all target frameworks including net10.0-browserwasm
        Environment.SetEnvironmentVariable("UNO_SINGLE_TARGET", "false");

        // Explicit restore for all frameworks to ensure project.assets.json is complete
        // Use root nuget.config to avoid conflicts with submodule configs
        context.Information("Restoring packages for all frameworks...");
        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        restoreSettings.MSBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        restoreSettings.MSBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "false" };
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        // Ensure UNO_SINGLE_TARGET is set for publish as well (in case it triggers implicit restore)
        msBuildSettings.Properties["UNO_SINGLE_TARGET"] = new[] { "false" };

        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            Framework = "net10.0-browserwasm",
            OutputDirectory = outputDir,
            MSBuildSettings = msBuildSettings
        };

        context.DotNetPublish(context.CsprojPath, settings);

        context.Information($"WebAssembly build completed. Output: {outputDir}");
    }
}
