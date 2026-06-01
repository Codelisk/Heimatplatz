using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployAstro")]
[IsDependentOn(typeof(BuildAstroTask))]
public sealed class DeployAstroTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy Astro Web Task (Azure Static Web Apps) ===");

        var distDir = Path.Combine(context.ProjectDirectory, "src", "web", "dist");

        if (!Directory.Exists(distDir))
        {
            throw new DirectoryNotFoundException($"Astro build output not found: {distDir}. Did the Astro build succeed?");
        }

        if (string.IsNullOrEmpty(context.AzureStaticWebAppsApiToken))
        {
            context.Warning("AZURE_STATIC_WEB_APPS_API_TOKEN not configured. Skipping deployment.");
            context.Information("To enable deployment, set the AZURE_STATIC_WEB_APPS_API_TOKEN environment variable or configure it in appsettings.json");
            return;
        }

        context.Information($"Deploying from: {distDir}");

        // Use Azure Static Web Apps CLI (swa) for deployment
        var processInfo = new ProcessStartInfo
        {
            FileName = "swa",
            Arguments = $"deploy {distDir} --deployment-token {context.AzureStaticWebAppsApiToken} --env production",
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

        // WICHTIG: stdout UND stderr gleichzeitig (asynchron) leeren, sonst Deadlock,
        // sobald die gespraechige SWA-CLI einen Pipe-Puffer fuellt (siehe BuildAstroTask).
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(20 * 60_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException("SWA CLI hat das Zeitlimit von 20 Minuten ueberschritten und wurde abgebrochen.");
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

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

        context.Information("Astro web deployment to Azure Static Web Apps completed!");
    }
}
