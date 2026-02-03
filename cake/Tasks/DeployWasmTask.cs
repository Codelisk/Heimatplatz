using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployWasm")]
[IsDependentOn(typeof(BuildWasmTask))]
public sealed class DeployWasmTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy WebAssembly Task (Azure Static Web Apps) ===");

        var artifactsDir = Path.Combine(context.ProjectDirectory, "artifacts", "wasm");
        var wwwrootDir = Path.Combine(artifactsDir, "wwwroot");

        if (!Directory.Exists(wwwrootDir))
        {
            throw new DirectoryNotFoundException($"wwwroot directory not found in {artifactsDir}. Did the WASM build succeed?");
        }

        if (string.IsNullOrEmpty(context.AzureStaticWebAppsApiToken))
        {
            context.Warning("AZURE_STATIC_WEB_APPS_API_TOKEN not configured. Skipping deployment.");
            context.Information("To enable deployment, set the AZURE_STATIC_WEB_APPS_API_TOKEN environment variable or configure it in appsettings.json");
            return;
        }

        context.Information($"Deploying from: {wwwrootDir}");

        // Use Azure Static Web Apps CLI (swa) for deployment
        var processInfo = new ProcessStartInfo
        {
            FileName = "swa",
            Arguments = $"deploy {wwwrootDir} --deployment-token {context.AzureStaticWebAppsApiToken} --env production",
            WorkingDirectory = context.ProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start SWA CLI. Make sure Azure Static Web Apps CLI is installed: npm install -g @azure/static-web-apps-cli");
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
            throw new InvalidOperationException($"SWA CLI failed with exit code {process.ExitCode}. " +
                "Make sure Azure Static Web Apps CLI is installed: npm install -g @azure/static-web-apps-cli");
        }

        context.Information("WebAssembly deployment to Azure Static Web Apps completed!");
    }
}
