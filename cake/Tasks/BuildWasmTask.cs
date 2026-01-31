using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
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

        // Force single-target mode so MSBuild only resolves WASM dependencies
        Environment.SetEnvironmentVariable("UNO_SINGLE_TARGET", "browserwasm");

        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            Framework = "net10.0-browserwasm",
            OutputDirectory = outputDir
        };

        context.DotNetPublish(context.CsprojPath, settings);

        context.Information($"WebAssembly build completed. Output: {outputDir}");
    }
}
