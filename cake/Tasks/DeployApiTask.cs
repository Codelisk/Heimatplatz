using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Deployt die ASP.NET-API auf den Hetzner-Server (Prod + Test-API):
///  1. Quellbaum als tar packen (src/api + Root-Props/nuget.config, ohne bin/obj)
///  2. per scp hochladen, am Server /srv/heimatplatz/src/api KOMPLETT ersetzen
///     (nie einzelne Dateien - siehe "12 Tage veralteter Frankenstein-Mix"-Vorfall)
///  3. docker compose build + up fuer api und api-test
///  4. Health-Gate auf beide /health-Endpunkte
/// Ersetzt den bisher manuellen tar/scp/ssh-Ablauf. Ohne konfigurierten
/// Hetzner:SshKeyPath wird der Standard-SSH-Key des Benutzers verwendet
/// (lokaler Lauf); CI setzt HETZNER_SSH_KEY_PATH.
/// </summary>
[TaskName("DeployApi")]
public sealed class DeployApiTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy API (Hetzner) ===");

        if (string.IsNullOrEmpty(context.HetznerHost) || string.IsNullOrEmpty(context.HetznerUser))
        {
            throw new InvalidOperationException(
                "Hetzner:Host und Hetzner:User muessen konfiguriert sein (appsettings.json oder HETZNER_HOST/HETZNER_USER).");
        }

        // Lokal: leerer SshKeyPath => Standard-Key des Benutzers (~/.ssh); CI setzt den Pfad.
        var keyOption = !string.IsNullOrEmpty(context.HetznerSshKeyPath) && File.Exists(context.HetznerSshKeyPath)
            ? $"-i {context.HetznerSshKeyPath} "
            : string.Empty;
        var sshOptions = $"{keyOption}-o StrictHostKeyChecking=accept-new -o BatchMode=yes";
        var target = $"{context.HetznerUser}@{context.HetznerHost}";

        // 1. Quellen packen (immer der KOMPLETTE Baum)
        var tarPath = Path.Combine(Path.GetTempPath(), "heimatplatz-api-deploy.tgz");
        if (File.Exists(tarPath))
        {
            File.Delete(tarPath);
        }

        context.Information("Packe API-Quellen...");
        AstroWeb.RunProcess(
            context,
            "tar",
            $"--exclude=bin --exclude=obj -czf \"{tarPath}\" src/api Directory.Build.props Directory.Packages.props nuget.config",
            context.ProjectDirectory,
            label: "tar (api sources)");

        // 2. Hochladen
        context.Information($"Lade Quellen auf {context.HetznerHost} hoch...");
        AstroWeb.RunProcess(
            context,
            "scp",
            $"{sshOptions} \"{tarPath}\" {target}:/tmp/api-deploy.tgz",
            context.ProjectDirectory,
            label: "scp (api sources)");

        // 3. Quellbaum ersetzen, Image bauen, Container hochfahren
        // builder prune: Der Docker-Build-Cache waechst pro Deploy und hat am 22.7.2026 die
        // 38-GB-Platte auf 100% gefuellt (Postgres rejecting connections, Prod-API ~5 Min down).
        // --keep-storage behaelt genug Cache fuer schnelle Folge-Builds.
        var remoteScript =
            "cd /srv/heimatplatz && " +
            "rm -rf src/api Directory.Build.props Directory.Packages.props nuget.config && " +
            "tar -xzf /tmp/api-deploy.tgz -C /srv/heimatplatz && rm /tmp/api-deploy.tgz && " +
            "cd deploy/hetzner && docker compose build api api-test && docker compose up -d api api-test && " +
            "docker builder prune -f --keep-storage=8GB > /dev/null && docker image prune -f > /dev/null";

        context.Information("Baue und starte api + api-test auf dem Server (kann einige Minuten dauern)...");
        AstroWeb.RunProcess(
            context,
            "ssh",
            $"{sshOptions} {target} \"{remoteScript}\"",
            context.ProjectDirectory,
            label: "ssh (api build + up)",
            timeoutMinutes: 20);

        File.Delete(tarPath);

        // 4. Smoke-Check: erst wenn beide APIs antworten, ist der Deploy gruen
        DeployHealth.WaitFor(context, context.Configuration["Hetzner:ApiHealthUrl"], timeoutSeconds: 180);
        DeployHealth.WaitFor(context, context.Configuration["Hetzner:ApiTestHealthUrl"], timeoutSeconds: 180);

        context.Information("API-Deploy abgeschlossen und verifiziert!");
    }
}
