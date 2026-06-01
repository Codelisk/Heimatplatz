using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildAstro")]
public sealed class BuildAstroTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Build Astro Web Task ===");

        var webDir = Path.Combine(context.ProjectDirectory, "src", "web");
        if (!Directory.Exists(webDir))
        {
            throw new DirectoryNotFoundException($"Astro web project not found: {webDir}");
        }

        // 1) Reproducible dependency install from package-lock.json
        context.Information($"Installing npm dependencies in {webDir}...");
        RunProcess(context, "npm", "ci", webDir, environment: null);

        // 2) Build the static site. PUBLIC_API_BASE_URL is read by src/config/site.ts at build time.
        var buildEnv = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(context.ApiBaseUrl))
        {
            buildEnv["PUBLIC_API_BASE_URL"] = context.ApiBaseUrl;
            context.Information($"Building with PUBLIC_API_BASE_URL={context.ApiBaseUrl}");
        }

        context.Information("Building Astro static site (npm run build)...");
        RunProcess(context, "npm", "run build", webDir, buildEnv);

        var distDir = Path.Combine(webDir, "dist");
        if (!Directory.Exists(distDir))
        {
            throw new DirectoryNotFoundException($"Astro build output not found: {distDir}. Did the build succeed?");
        }

        context.Information($"Astro build completed. Output: {distDir}");
    }

    private static void RunProcess(
        BuildContext context,
        string fileName,
        string arguments,
        string workingDirectory,
        IDictionary<string, string>? environment,
        int timeoutMinutes = 30)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (environment != null)
        {
            foreach (var kvp in environment)
            {
                processInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process '{fileName}'. Make sure Node.js/npm is installed and on PATH.");
        }

        // WICHTIG: stdout UND stderr gleichzeitig (asynchron) leeren. Sequenzielles
        // ReadToEnd() (erst stdout, dann stderr) blockiert, sobald ein gespraechiger
        // Prozess wie 'astro build' den stderr-Pipe-Puffer fuellt, waehrend wir noch
        // auf stdout-EOF warten -> klassischer Deadlock (Build haengt ohne Ausgabe).
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMinutes * 60_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException(
                $"'{fileName} {arguments}' hat das Zeitlimit von {timeoutMinutes} Minuten ueberschritten und wurde abgebrochen.");
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
            throw new InvalidOperationException($"'{fileName} {arguments}' failed with exit code {process.ExitCode}.");
        }
    }
}
