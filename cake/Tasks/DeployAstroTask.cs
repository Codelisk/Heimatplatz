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
        context.Information("=== Deploy Astro Web Task (Hetzner via rsync, SSR-Node-Container) ===");

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

        var sshCommand = $"ssh -i {context.HetznerSshKeyPath} -o StrictHostKeyChecking=accept-new";
        var target = $"{context.HetznerUser}@{context.HetznerHost}:{context.HetznerWebRoot}/";
        context.Information($"Deploying {distDir} -> {target}");

        // rsync --delete haelt das Zielverzeichnis exakt auf dem Stand des Builds.
        // dist/ enthaelt das komplette SSR-Bundle (server/entry.mjs + client-Assets);
        // der Node-Container laedt es nach dem Restart unten.
        RunProcess(
            context,
            "rsync",
            $"-az --delete -e \"{sshCommand}\" \"{distDir}/\" \"{target}\"",
            "rsync (dist)");

        // Compose + Caddyfile mitdeployen: /srv/heimatplatz ist KEIN Git-Checkout,
        // sondern ein Datei-Abzug - die Stack-Definition kommt daher aus dem CI-Checkout.
        // Bewusst ohne --delete (im Zielordner liegen .env und secrets/).
        var deployDir = Path.Combine(context.ProjectDirectory, "deploy", "hetzner");
        var stackTarget = $"{context.HetznerUser}@{context.HetznerHost}:/srv/heimatplatz/deploy/hetzner/";
        RunProcess(
            context,
            "rsync",
            $"-az -e \"{sshCommand}\" \"{Path.Combine(deployDir, "docker-compose.yml")}\" \"{Path.Combine(deployDir, "Caddyfile")}\" \"{stackTarget}\"",
            "rsync (stack config)");

        // web-Container (er)stellen und neu starten, damit das frische Bundle laeuft;
        // Caddy laedt seine (ggf. geaenderte) Konfiguration idempotent neu.
        var remoteScript =
            "cd /srv/heimatplatz/deploy/hetzner && " +
            "docker compose up -d web && docker compose restart web && " +
            "docker compose exec -T caddy caddy reload --config /etc/caddy/Caddyfile";
        context.Information("Restarting SSR web container on the server...");
        RunProcess(
            context,
            "ssh",
            $"-i {context.HetznerSshKeyPath} -o StrictHostKeyChecking=accept-new {context.HetznerUser}@{context.HetznerHost} \"{remoteScript}\"",
            "ssh (web restart)");

        context.Information("Astro SSR deployment to Hetzner completed!");
    }

    private static void RunProcess(BuildContext context, string fileName, string arguments, string label)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
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
            throw new InvalidOperationException($"Failed to start {fileName}. Make sure rsync and ssh are installed (Standard auf ubuntu-latest Runnern).");
        }

        // WICHTIG: stdout UND stderr gleichzeitig (asynchron) leeren, sonst Deadlock,
        // sobald ein Pipe-Puffer volllaeuft (siehe BuildAstroTask).
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(10 * 60_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"{label} hat das Zeitlimit von 10 Minuten ueberschritten und wurde abgebrochen.");
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(output))
        {
            context.Information(output);
        }
        if (!string.IsNullOrEmpty(error))
        {
            context.Warning(error);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{label} failed with exit code {process.ExitCode}.");
        }
    }
}
