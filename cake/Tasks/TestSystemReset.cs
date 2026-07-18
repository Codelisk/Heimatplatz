using System.Text.Json;
using Cake.Common.Diagnostics;

namespace Build.Tasks;

/// <summary>
/// Setzt das Test-System (Test-Postgres auf Hetzner + api-test) vor einem
/// Screenshot-Lauf zurueck: Volume weg, Container frisch, AutoMigrate + Seeding
/// laufen beim api-test-Start automatisch. Danach wird gewartet, bis die Test-API
/// gesund ist UND die Seed-Immobilien tatsaechlich abrufbar sind - erst dann sind
/// deterministische Screenshots moeglich (kuratierte Seed-Fotos, feste Favoriten).
/// </summary>
public static class TestSystemReset
{
    public static void Run(BuildContext context, string testApiBaseUrl)
    {
        context.Information("=== Test-System zuruecksetzen (frischer Seed fuer deterministische Screenshots) ===");

        var host = context.HetznerHost;
        var user = context.HetznerUser;
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user))
        {
            context.Warning("Hetzner:Host/User nicht konfiguriert - Test-DB-Reset uebersprungen (Screenshots zeigen den aktuellen DB-Stand).");
            return;
        }

        // Lokal: leerer SshKeyPath => Standard-Key des Benutzers (~/.ssh); CI setzt den Pfad.
        var hasConfiguredKey = !string.IsNullOrEmpty(context.HetznerSshKeyPath) && File.Exists(context.HetznerSshKeyPath);
        var sshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
        var hasDefaultKey = File.Exists(Path.Combine(sshDir, "id_ed25519")) || File.Exists(Path.Combine(sshDir, "id_rsa"));
        if (!hasConfiguredKey && !hasDefaultKey)
        {
            // Z.B. Codemagic ohne Hetzner-Key: nicht hart scheitern, nur klar warnen
            context.Warning("Kein SSH-Key fuer Hetzner verfuegbar - Test-DB-Reset uebersprungen (Screenshots zeigen den aktuellen DB-Stand).");
            return;
        }

        var keyOption = hasConfiguredKey ? $"-i {context.HetznerSshKeyPath} " : string.Empty;
        var sshOptions = $"{keyOption}-o StrictHostKeyChecking=accept-new -o BatchMode=yes";

        var remoteScript =
            "cd /srv/heimatplatz/deploy/hetzner && " +
            "docker compose -f docker-compose.testdb.yml down -v && " +
            "docker compose -f docker-compose.testdb.yml up -d && " +
            "sleep 5 && docker compose restart api-test";

        AstroWeb.RunProcess(
            context,
            "ssh",
            $"{sshOptions} {user}@{host} \"{remoteScript}\"",
            context.ProjectDirectory,
            label: "ssh (testdb reset + api-test restart)",
            timeoutMinutes: 10);

        WaitForSeededData(context, testApiBaseUrl);
    }

    /// <summary>
    /// Pollt die Test-API, bis der Seed durch ist (Health reicht nicht - AutoMigrate
    /// und Seeding laufen nach dem Container-Start noch einige Sekunden).
    /// </summary>
    private static void WaitForSeededData(BuildContext context, string apiBaseUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var url = $"{apiBaseUrl.TrimEnd('/')}/api/properties?PageSize=50";
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(4);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var json = http.GetStringAsync(url).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Properties", out var properties)
                    && properties.GetArrayLength() >= 13)
                {
                    context.Information($"Test-API geseedet: {properties.GetArrayLength()} Immobilien verfuegbar.");
                    return;
                }
                context.Information($"Warte auf Seed... (aktuell {(properties.ValueKind == JsonValueKind.Array ? properties.GetArrayLength() : 0)} Immobilien)");
            }
            catch (Exception ex)
            {
                context.Information($"Test-API noch nicht bereit ({ex.GetType().Name}) - warte...");
            }
            Thread.Sleep(5000);
        }

        throw new InvalidOperationException(
            "Test-API hat nach dem Reset nicht rechtzeitig geseedete Daten geliefert - Screenshot-Lauf abgebrochen (waere nicht deterministisch).");
    }
}
