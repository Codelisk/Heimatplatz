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
        context.Information("=== Deploy Astro Web Task (Hetzner via rsync) ===");

        var distDir = Path.Combine(context.ProjectDirectory, "src", "web", "dist");

        if (!Directory.Exists(distDir))
        {
            throw new DirectoryNotFoundException($"Astro build output not found: {distDir}. Did the Astro build succeed?");
        }

        if (string.IsNullOrEmpty(context.HetznerSshKeyPath) || !File.Exists(context.HetznerSshKeyPath))
        {
            context.Warning("HETZNER_SSH_KEY_PATH not configured or key file missing. Skipping deployment.");
            context.Information("To enable deployment, set the HETZNER_SSH_KEY_PATH environment variable (private key file) or configure Hetzner:SshKeyPath in appsettings.json");
            return;
        }

        if (string.IsNullOrEmpty(context.HetznerHost) || string.IsNullOrEmpty(context.HetznerUser) || string.IsNullOrEmpty(context.HetznerWebRoot))
        {
            throw new InvalidOperationException("Hetzner:Host, Hetzner:User and Hetzner:WebRoot must be configured (appsettings.json or HETZNER_HOST/HETZNER_USER/HETZNER_WEB_ROOT).");
        }

        var target = $"{context.HetznerUser}@{context.HetznerHost}:{context.HetznerWebRoot}/";
        context.Information($"Deploying {distDir} -> {target}");

        // rsync --delete haelt das Zielverzeichnis exakt auf dem Stand des Builds
        // (alte Assets verschwinden). Trailing Slash am Quellpfad = Inhalt kopieren.
        var sshCommand = $"ssh -i {context.HetznerSshKeyPath} -o StrictHostKeyChecking=accept-new";
        var arguments = $"-az --delete -e \"{sshCommand}\" \"{distDir}/\" \"{target}\"";

        var processInfo = new ProcessStartInfo
        {
            FileName = "rsync",
            Arguments = arguments,
            WorkingDirectory = context.ProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start rsync. Make sure rsync and ssh are installed (Standard auf ubuntu-latest Runnern).");
        }

        // WICHTIG: stdout UND stderr gleichzeitig (asynchron) leeren, sonst Deadlock,
        // sobald ein Pipe-Puffer volllaeuft (siehe BuildAstroTask).
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(10 * 60_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException("rsync hat das Zeitlimit von 10 Minuten ueberschritten und wurde abgebrochen.");
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
            throw new InvalidOperationException($"rsync failed with exit code {process.ExitCode}.");
        }

        context.Information("Astro web deployment to Hetzner completed!");
    }
}
