using System.Diagnostics;
using Cake.Common.Diagnostics;

namespace Build.Tasks;

/// <summary>
/// Gemeinsame Build- und Deploy-Logik fuer das Astro-Web-SSR-Bundle (Hetzner).
/// Von BuildAstro/DeployAstro (Prod) und DeployAstroTest (Test-Web) genutzt -
/// die einzigen Unterschiede sind die beim Build eingebettete PUBLIC_API_BASE_URL,
/// das Ziel-Verzeichnis auf dem Server und der neu zu startende Container.
/// </summary>
public static class AstroWeb
{
    /// <summary>
    /// npm-Abhaengigkeiten installieren und das SSR-Bundle bauen. Die uebergebene
    /// <paramref name="apiBaseUrl"/> wird als PUBLIC_API_BASE_URL in die Client-Skripte
    /// eingebettet; serverseitige SSR-Fetches nutzen zur Laufzeit API_BASE_URL_SERVER
    /// (docker-compose). <paramref name="rybbitSiteId"/> aktiviert das Rybbit-Tracking-
    /// Snippet (BaseLayout.astro) - Prod und Test-Web uebergeben je eine eigene Site-Id;
    /// leer lassen (z.B. lokal) baut ein Bundle ohne Tracking.
    /// </summary>
    public static void Build(BuildContext context, string? apiBaseUrl, string? rybbitSiteId = null)
    {
        context.Information("=== Build Astro Web ===");

        var webDir = Path.Combine(context.ProjectDirectory, "src", "web");
        if (!Directory.Exists(webDir))
        {
            throw new DirectoryNotFoundException($"Astro web project not found: {webDir}");
        }

        // 1) Reproducible dependency install from package-lock.json
        context.Information($"Installing npm dependencies in {webDir}...");
        RunProcess(context, "npm", "ci", webDir);

        // 2) Validate + build the SSR bundle ('validate' = astro check && astro build).
        var buildEnv = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            buildEnv["PUBLIC_API_BASE_URL"] = apiBaseUrl;
            context.Information($"Building with PUBLIC_API_BASE_URL={apiBaseUrl}");
        }
        if (!string.IsNullOrEmpty(rybbitSiteId))
        {
            buildEnv["PUBLIC_RYBBIT_SITE_ID"] = rybbitSiteId;
            context.Information("Building with PUBLIC_RYBBIT_SITE_ID set (Rybbit-Tracking aktiv)");
        }

        context.Information("Validating and building Astro SSR bundle (npm run validate)...");
        RunProcess(context, "npm", "run validate", webDir, buildEnv);

        var distDir = Path.Combine(webDir, "dist");
        if (!Directory.Exists(distDir))
        {
            throw new DirectoryNotFoundException($"Astro build output not found: {distDir}. Did the build succeed?");
        }

        context.Information($"Astro build completed. Output: {distDir}");
    }

    /// <summary>
    /// Das gebaute Bundle per rsync auf den Hetzner-Server bringen und den passenden
    /// SSR-Container neu starten. <paramref name="webRoot"/> ist das Ziel-Verzeichnis
    /// (Prod: /srv/heimatplatz-web, Test: /srv/heimatplatz-web-test),
    /// <paramref name="containerName"/> der zu (er)stellende/neu zu startende Compose-Service.
    /// </summary>
    public static void Deploy(BuildContext context, string webRoot, string containerName)
    {
        context.Information($"=== Deploy Astro Web -> {containerName} ({webRoot}) ===");

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

        if (string.IsNullOrEmpty(context.HetznerHost) || string.IsNullOrEmpty(context.HetznerUser) || string.IsNullOrEmpty(webRoot))
        {
            throw new InvalidOperationException("Hetzner:Host, Hetzner:User and the web root must be configured (appsettings.json or HETZNER_HOST/HETZNER_USER/HETZNER_WEB_ROOT[_TEST]).");
        }

        var webDir = Path.Combine(context.ProjectDirectory, "src", "web");
        var sshCommand = $"ssh -i {context.HetznerSshKeyPath} -o StrictHostKeyChecking=accept-new";
        var target = $"{context.HetznerUser}@{context.HetznerHost}:{webRoot}/";
        context.Information($"Deploying {distDir} -> {target}");

        // Der @astrojs/node-Standalone-Server buendelt NICHT alle Abhaengigkeiten
        // (piccolore, devalue, send, unstorage, ... bleiben externe Imports) -
        // die Production-node_modules muessen daher mit aufs Ziel. devDeps vorher
        // entfernen (Build ist bereits gelaufen, danach werden sie nicht mehr gebraucht).
        context.Information("Pruning dev dependencies (npm prune --omit=dev)...");
        RunProcess(context, "npm", "prune --omit=dev", webDir);

        // rsync --delete haelt das Zielverzeichnis exakt auf dem Stand des Builds.
        // dist/ enthaelt das SSR-Bundle (server/entry.mjs + client-Assets); das separat
        // gesyncte node_modules im Ziel wird per --exclude vor --delete geschuetzt.
        // client/tiles ausschliessen: die Karten-Tiles (public/tiles, ~250 MB, gitignored)
        // liegen nur fuer die lokale Entwicklung im public-Ordner - auf dem Server liefert
        // Caddy sie direkt aus /srv/heimatplatz/deploy/hetzner/map-tiles (handle_path /tiles).
        RunProcess(
            context,
            "rsync",
            $"-az --delete --exclude=/node_modules --exclude=/client/tiles -e \"{sshCommand}\" \"{distDir}/\" \"{target}\"",
            context.ProjectDirectory,
            label: "rsync (dist)");

        RunProcess(
            context,
            "rsync",
            $"-az --delete -e \"{sshCommand}\" \"{Path.Combine(webDir, "node_modules")}/\" \"{target}node_modules/\"",
            context.ProjectDirectory,
            label: "rsync (node_modules)");

        // Compose + Caddyfile mitdeployen: /srv/heimatplatz ist KEIN Git-Checkout,
        // sondern ein Datei-Abzug - die Stack-Definition kommt daher aus dem CI-Checkout.
        // Bewusst ohne --delete (im Zielordner liegen .env und secrets/). --inplace ist
        // Pflicht: Das Caddyfile ist als Einzeldatei in den Container gemountet - ein
        // rsync-Rename (neuer Inode) waere im Container unsichtbar und der Reload wirkungslos.
        var deployDir = Path.Combine(context.ProjectDirectory, "deploy", "hetzner");
        var stackTarget = $"{context.HetznerUser}@{context.HetznerHost}:/srv/heimatplatz/deploy/hetzner/";
        RunProcess(
            context,
            "rsync",
            $"-az --inplace -e \"{sshCommand}\" \"{Path.Combine(deployDir, "docker-compose.yml")}\" \"{Path.Combine(deployDir, "Caddyfile")}\" \"{stackTarget}\"",
            context.ProjectDirectory,
            label: "rsync (stack config)");

        // Ziel-Container (er)stellen und neu starten, damit das frische Bundle laeuft;
        // Caddy laedt seine (ggf. geaenderte) Konfiguration idempotent neu.
        var remoteScript =
            "cd /srv/heimatplatz/deploy/hetzner && " +
            $"docker compose up -d {containerName} && docker compose restart {containerName} && " +
            "docker compose exec -T caddy caddy reload --config /etc/caddy/Caddyfile";
        context.Information($"Restarting SSR container '{containerName}' on the server...");
        RunProcess(
            context,
            "ssh",
            $"-i {context.HetznerSshKeyPath} -o StrictHostKeyChecking=accept-new {context.HetznerUser}@{context.HetznerHost} \"{remoteScript}\"",
            context.ProjectDirectory,
            label: $"ssh ({containerName} restart)");

        context.Information($"Astro SSR deployment to Hetzner ({containerName}) completed!");
    }

    internal static void RunProcess(
        BuildContext context,
        string fileName,
        string arguments,
        string workingDirectory,
        IDictionary<string, string>? environment = null,
        string? label = null,
        int timeoutMinutes = 30)
    {
        label ??= $"{fileName} {arguments}";

        // CreateProcess loest nur .exe auf - npm ist unter Windows eine .cmd und
        // braucht den ABSOLUTEN Pfad, sonst zeigt %~dp0 in npm.cmd auf das
        // Arbeitsverzeichnis statt auf das Node-Installationsverzeichnis.
        if (OperatingSystem.IsWindows() && fileName == "npm")
        {
            fileName = Environment.GetEnvironmentVariable("PATH")?
                .Split(Path.PathSeparator)
                .Select(dir => Path.Combine(dir.Trim(), "npm.cmd"))
                .FirstOrDefault(File.Exists) ?? "npm.cmd";
        }

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
            throw new InvalidOperationException($"Failed to start '{fileName}'. Make sure the required tool (Node.js/npm, rsync, ssh) is installed and on PATH.");
        }

        // WICHTIG: stdout UND stderr gleichzeitig (asynchron) leeren, sonst Deadlock,
        // sobald ein gespraechiger Prozess einen Pipe-Puffer fuellt.
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMinutes * 60_000))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"{label} hat das Zeitlimit von {timeoutMinutes} Minuten ueberschritten und wurde abgebrochen.");
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
