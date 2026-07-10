using System.Diagnostics;
using System.Text.Json;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build.Tasks;

/// <summary>
/// Erzeugt deterministische App-Store-Screenshots im iOS-Simulator:
/// Simulator-Build (unsigniert), lokale Test-API gegen die Hetzner-Test-Postgres
/// (geseedete Daten, Port 5433), Boot der konfigurierten Geraete, Status-Bar-Override
/// (09:41, volle Batterie, WLAN), App-Start pro konfigurierter Shell-Route im
/// Screenshot-Modus mit Auto-Login (SIMCTL_CHILD_SCREENSHOT_*) und Screenshot via "simctl io".
/// Konfiguration: Sektion "iOS:Screenshots" in appsettings.json; das Test-DB-Passwort kommt
/// aus TESTDB_PASSWORD (env) oder iOS:Screenshots:TestDbPassword (appsettings.Local.json).
/// Ausgabe: artifacts/screenshots/ios/&lt;locale&gt;/&lt;device&gt;_&lt;shot&gt;.png
/// (deliver-kompatible Locale-Ordner, Geraetetyp erkennt deliver an der Aufloesung).
/// </summary>
[TaskName("IosScreenshots")]
public sealed class IosScreenshotsTask : FrostingTask<BuildContext>
{
    private const string SimulatorRuntimeIdentifier = "iossimulator-arm64";

    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS screenshots task can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== iOS App Store Screenshots ===");

        var config = LoadConfig(context);
        var appBundle = BuildSimulatorApp(context);

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "screenshots", "ios", config.Locale);
        Directory.CreateDirectory(outputDir);

        Process? apiProcess = null;
        var captured = new List<string>();
        try
        {
            if (config.RunLocalTestApi)
            {
                apiProcess = StartLocalTestApi(context, config);
            }

            foreach (var deviceName in config.Devices)
            {
                var device = ResolveSimulator(context, deviceName);
                context.Information($"Device: {device.Name} ({device.Udid}, {device.Runtime})");
                captured.AddRange(CaptureDevice(context, config, device, appBundle, outputDir));
            }
        }
        finally
        {
            if (apiProcess is { HasExited: false })
            {
                context.Information("Stopping local test API...");
                apiProcess.Kill(entireProcessTree: true);
                apiProcess.WaitForExit();
            }
            apiProcess?.Dispose();
        }

        context.Information($"Captured {captured.Count} screenshots into {outputDir}:");
        foreach (var file in captured)
        {
            context.Information($"  {Path.GetFileName(file)}");
        }
    }

    private ScreenshotConfig LoadConfig(BuildContext context)
    {
        var section = context.Configuration.GetSection("iOS:Screenshots");

        var devices = section.GetSection("Devices").GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
        if (devices.Count == 0)
        {
            devices = ["iPhone 17 Pro Max", "iPad Pro 13-inch"];
        }

        var defaultDelay = int.TryParse(section["LaunchDelaySeconds"], out var d) ? d : 12;

        var shots = section.GetSection("Shots").GetChildren()
            .Select(c => new Shot(
                c["Name"] ?? throw new InvalidOperationException("iOS:Screenshots:Shots entry without Name"),
                c["Route"] ?? string.Empty,
                int.TryParse(c["DelaySeconds"], out var sd) ? sd : defaultDelay))
            .ToList();
        if (shots.Count == 0)
        {
            shots = [new Shot("01_startseite", string.Empty, defaultDelay)];
        }

        // Default true: Screenshots laufen gegen die lokale Test-API mit geseedeten Daten
        var runLocalTestApi = !bool.TryParse(section["RunLocalTestApi"], out var runApi) || runApi;

        var apiBaseUrl = section["ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = runLocalTestApi ? "http://localhost:5292" : null;
        }

        return new ScreenshotConfig(
            Locale: section["Locale"] ?? "de-DE",
            AppleLanguage: section["AppleLanguage"] ?? "de",
            AppleLocale: section["AppleLocale"] ?? "de_AT",
            ApiBaseUrl: apiBaseUrl,
            RunLocalTestApi: runLocalTestApi,
            TestDbConnectionString: section["TestDbConnectionString"] ?? string.Empty,
            TestDbPassword: FirstNonEmpty(
                Environment.GetEnvironmentVariable("TESTDB_PASSWORD"),
                section["TestDbPassword"]),
            LoginEmail: FirstNonEmpty(
                Environment.GetEnvironmentVariable("SCREENSHOT_LOGIN_EMAIL"),
                section["LoginEmail"]),
            LoginPassword: FirstNonEmpty(
                Environment.GetEnvironmentVariable("SCREENSHOT_LOGIN_PASSWORD"),
                section["LoginPassword"]),
            Devices: devices,
            Shots: shots);
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    /// <summary>
    /// Startet die API aus dem Repo lokal gegen die Test-Postgres auf dem Hetzner-Server.
    /// Der iOS-Simulator teilt das Host-Netzwerk, erreicht die API also unter localhost.
    /// </summary>
    private Process StartLocalTestApi(BuildContext context, ScreenshotConfig config)
    {
        if (string.IsNullOrEmpty(config.TestDbPassword))
        {
            throw new InvalidOperationException(
                "Test-DB password not configured. Set TESTDB_PASSWORD env var or iOS:Screenshots:TestDbPassword in appsettings.Local.json.");
        }
        if (string.IsNullOrEmpty(config.TestDbConnectionString))
        {
            throw new InvalidOperationException("iOS:Screenshots:TestDbConnectionString not configured.");
        }

        var apiCsproj = Path.Combine(context.ProjectDirectory, "src", "api", "src", "Heimatplatz.Api", "Heimatplatz.Api.csproj");

        context.Information("Building local test API...");
        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        context.DotNetRestore(apiCsproj, restoreSettings);
        context.DotNetBuild(apiCsproj, new DotNetBuildSettings { Configuration = "Release" });

        var apiDll = Path.Combine(Path.GetDirectoryName(apiCsproj)!, "bin", "Release", "net10.0", "Heimatplatz.Api.dll");
        if (!File.Exists(apiDll))
        {
            throw new InvalidOperationException($"API build output not found: {apiDll}");
        }

        context.Information($"Starting test API on {config.ApiBaseUrl} (test database)...");
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(apiDll),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processInfo.ArgumentList.Add(apiDll);
        processInfo.Environment["ASPNETCORE_URLS"] = config.ApiBaseUrl;
        processInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        processInfo.Environment["Database__Provider"] = "Postgres";
        processInfo.Environment["ConnectionStrings__DefaultConnection"] =
            $"{config.TestDbConnectionString};Password={config.TestDbPassword}";

        var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start test API process");

        // Output asynchron weglesen, sonst blockiert der API-Prozess bei vollem Pipe-Buffer
        process.OutputDataReceived += (_, e) => { if (e.Data != null) context.Debug(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) context.Debug(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            WaitForHealthy(context, process, $"{config.ApiBaseUrl}/health", TimeSpan.FromSeconds(120));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            process.Dispose();
            throw;
        }
        return process;
    }

    private static void WaitForHealthy(BuildContext context, Process apiProcess, string healthUrl, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (apiProcess.HasExited)
            {
                throw new InvalidOperationException($"Test API exited prematurely with code {apiProcess.ExitCode}");
            }

            try
            {
                var response = http.GetAsync(healthUrl).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    context.Information("Test API is healthy.");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // API bindet den Port erst nach Startup + AutoMigrate - weiter pollen
            }
            catch (TaskCanceledException)
            {
            }

            Thread.Sleep(1000);
        }

        throw new InvalidOperationException($"Test API did not become healthy within {timeout.TotalSeconds}s ({healthUrl})");
    }

    private string BuildSimulatorApp(BuildContext context)
    {
        context.Information($"Building simulator app ({SimulatorRuntimeIdentifier})...");

        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        msBuildSettings.Properties["RuntimeIdentifier"] = new[] { SimulatorRuntimeIdentifier };
        // Simulator-Apps laufen unsigniert; ohne das sucht _DetectSigningIdentity wegen der
        // gesetzten Entitlements.plist auf der leeren CI-Keychain nach Zertifikaten und bricht ab
        msBuildSettings.Properties["EnableCodeSigning"] = new[] { "false" };

        context.DotNetBuild(context.CsprojPath, new DotNetBuildSettings
        {
            Configuration = "Release",
            Framework = "net10.0-ios",
            MSBuildSettings = msBuildSettings
        });

        var binDir = Path.Combine(
            Path.GetDirectoryName(context.CsprojPath)!,
            "bin", "Release", "net10.0-ios", SimulatorRuntimeIdentifier);

        var appBundle = Directory.Exists(binDir)
            ? Directory.GetDirectories(binDir, "*.app").FirstOrDefault()
            : null;
        if (appBundle == null)
        {
            throw new InvalidOperationException($"No .app bundle found under {binDir}");
        }

        // Apple Silicon fuehrt nur signierten arm64-Code aus (mindestens ad-hoc) - ohne
        // Signatur killt der Kernel den Simulator-Prozess direkt nach dem Launch.
        // get-task-allow wie bei Xcode-Simulator-Builds mitgeben: "codesign --sign -"
        // ohne Entitlements strippt alle Rechte, was den Mono-Prozess ebenfalls killt.
        context.Information("Ad-hoc signing app bundle (with simulator entitlements)...");
        var entitlementsPath = Path.Combine(Path.GetTempPath(), "sim-entitlements.plist");
        File.WriteAllText(entitlementsPath,
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>com.apple.security.get-task-allow</key>
                <true/>
            </dict>
            </plist>
            """);
        // Erst alle eingebetteten Mach-O-Dateien einzeln signieren: "--deep" erfasst dylibs
        // im Bundle-Root nicht (MAUI legt die Mono-Runtime-dylibs dort ab, sie gelten als
        // Ressourcen) - dyld killt den Prozess sonst mit codesigning/invalid-page(2).
        RunProcess(context, "bash",
        [
            "-c",
            $"find \"{appBundle}\" -name '*.dylib' -exec codesign --force --sign - {{}} ';' && " +
            $"find \"{appBundle}\" -name '*.framework' -exec codesign --force --sign - {{}} ';'"
        ]);
        // Bundle selbst zuletzt signieren (versiegelt die Ressourcen inkl. der dylibs)
        RunXcrun(context, ["codesign", "--force", "--sign", "-", "--entitlements", entitlementsPath, appBundle]);

        context.Information($"App bundle: {appBundle}");
        return appBundle;
    }

    private SimulatorDevice ResolveSimulator(BuildContext context, string deviceName)
    {
        var (_, json) = RunXcrun(context, ["simctl", "list", "devices", "available", "--json"]);
        using var doc = JsonDocument.Parse(json);

        var candidates = new List<SimulatorDevice>();
        foreach (var runtime in doc.RootElement.GetProperty("devices").EnumerateObject())
        {
            // Nur iOS-Runtimes (keine watchOS/tvOS/visionOS-Simulatoren)
            if (!runtime.Name.Contains("SimRuntime.iOS", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var device in runtime.Value.EnumerateArray())
            {
                candidates.Add(new SimulatorDevice(
                    device.GetProperty("name").GetString() ?? string.Empty,
                    device.GetProperty("udid").GetString() ?? string.Empty,
                    device.GetProperty("state").GetString() ?? string.Empty,
                    runtime.Name));
            }
        }

        // Exakter Name gewinnt, sonst Teilstring-Match (z.B. "iPad Pro 13-inch" -> "(M4)");
        // bei mehreren Treffern die neueste Runtime (Runtime-Keys sortieren absteigend)
        var match = candidates
            .Where(c => c.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Runtime, StringComparer.Ordinal)
            .FirstOrDefault()
            ?? candidates
                .Where(c => c.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Runtime, StringComparer.Ordinal)
                .FirstOrDefault();

        if (match == null)
        {
            var available = string.Join(", ", candidates.Select(c => c.Name).Distinct().Order());
            throw new InvalidOperationException(
                $"Simulator '{deviceName}' not found. Available devices: {available}");
        }

        return match;
    }

    private IReadOnlyList<string> CaptureDevice(
        BuildContext context,
        ScreenshotConfig config,
        SimulatorDevice device,
        string appBundle,
        string outputDir)
    {
        var bootedByUs = false;
        if (!device.State.Equals("Booted", StringComparison.OrdinalIgnoreCase))
        {
            context.Information($"Booting {device.Name}...");
            RunXcrun(context, ["simctl", "boot", device.Udid]);
            bootedByUs = true;
        }
        // Blockiert bis der Simulator vollstaendig gebootet ist
        RunXcrun(context, ["simctl", "bootstatus", device.Udid, "-b"]);

        // Deterministische Status-Bar: 09:41, volle Batterie, WLAN
        RunXcrun(context,
        [
            "simctl", "status_bar", device.Udid, "override",
            "--time", "09:41",
            "--dataNetwork", "wifi",
            "--wifiMode", "active",
            "--wifiBars", "3",
            "--cellularMode", "notSupported",
            "--batteryState", "charged",
            "--batteryLevel", "100"
        ]);

        // Frische Installation fuer reproduzierbaren App-Zustand (keine alten Sessions/Prefs)
        RunXcrun(context, ["simctl", "uninstall", device.Udid, context.ApplicationId], throwOnError: false);
        RunXcrun(context, ["simctl", "install", device.Udid, appBundle]);

        var deviceSlug = ToSlug(device.Name);
        var captured = new List<string>();

        try
        {
            foreach (var shot in config.Shots)
            {
                context.Information($"[{device.Name}] {shot.Name} (Route: '{shot.Route}', {shot.DelaySeconds}s)...");

                RunXcrun(context, ["simctl", "terminate", device.Udid, context.ApplicationId], throwOnError: false);

                // SIMCTL_CHILD_* wird von simctl als Umgebungsvariable an den App-Prozess
                // durchgereicht (siehe ScreenshotMode.cs in der MAUI-App)
                var env = new Dictionary<string, string>
                {
                    ["SIMCTL_CHILD_SCREENSHOT_MODE"] = "1",
                    ["SIMCTL_CHILD_SCREENSHOT_ROUTE"] = shot.Route
                };
                if (!string.IsNullOrEmpty(config.ApiBaseUrl))
                {
                    env["SIMCTL_CHILD_HEIMATPLATZ_API_URL"] = config.ApiBaseUrl;
                }
                if (!string.IsNullOrEmpty(config.LoginEmail) && !string.IsNullOrEmpty(config.LoginPassword))
                {
                    env["SIMCTL_CHILD_SCREENSHOT_LOGIN_EMAIL"] = config.LoginEmail;
                    env["SIMCTL_CHILD_SCREENSHOT_LOGIN_PASSWORD"] = config.LoginPassword;
                }

                var (launchExit, _) = RunXcrun(context,
                [
                    "simctl", "launch", device.Udid, context.ApplicationId,
                    "-AppleLanguages", $"({config.AppleLanguage})",
                    "-AppleLocale", config.AppleLocale
                ], env: env, throwOnError: false);
                if (launchExit != 0)
                {
                    DumpLaunchDiagnostics(context, device);
                    throw new InvalidOperationException($"App launch failed for shot '{shot.Name}' (see diagnostics above)");
                }

                Thread.Sleep(TimeSpan.FromSeconds(shot.DelaySeconds));

                var file = Path.Combine(outputDir, $"{deviceSlug}_{shot.Name}.png");
                RunXcrun(context, ["simctl", "io", device.Udid, "screenshot", file]);

                // Liveness-Check: terminate schlaegt fehl, wenn die App beim Screenshot
                // nicht mehr lief - dann zeigt das PNG nur den Home-Screen
                var (terminateExit, _) = RunXcrun(context,
                    ["simctl", "terminate", device.Udid, context.ApplicationId], throwOnError: false);
                if (terminateExit != 0)
                {
                    DumpLaunchDiagnostics(context, device);
                    throw new InvalidOperationException(
                        $"App was no longer running when shot '{shot.Name}' was captured - startup crash? (see diagnostics above)");
                }

                captured.Add(file);
            }
        }
        finally
        {
            // ScreenshotMode-Statuszeilen der App aus dem os_log ziehen (Login/Navigation
            // sichtbar machen, auch wenn alle Shots "erfolgreich" waren)
            var (_, modeLog) = RunProcess(context, "bash",
            [
                "-c",
                $"xcrun simctl spawn {device.Udid} log show --last 10m --style compact " +
                "--predicate 'process == \"Heimatplatz.Maui\"' 2>/dev/null | grep -F '[ScreenshotMode]' | tail -30"
            ], throwOnError: false);
            context.Information($"[ScreenshotMode] app log ({device.Name}):\n{modeLog}");

            RunXcrun(context, ["simctl", "terminate", device.Udid, context.ApplicationId], throwOnError: false);
            if (bootedByUs)
            {
                RunXcrun(context, ["simctl", "shutdown", device.Udid], throwOnError: false);
            }
        }

        return captured;
    }

    private static string ToSlug(string name) =>
        new(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    /// <summary>
    /// Kippt Crash-Reports und das App-Log in das Build-Log, wenn die App nicht
    /// (mehr) laeuft - Simulator-Prozesse crashen auf dem Host, die Reports liegen
    /// daher in ~/Library/Logs/DiagnosticReports.
    /// </summary>
    private void DumpLaunchDiagnostics(BuildContext context, SimulatorDevice device)
    {
        context.Warning("Collecting crash diagnostics...");

        // Direktester Weg zum Managed-Stacktrace: Launch mit angebundener Konsole -
        // bei einem Startup-Crash landet die .NET-Exception auf stderr. timeout killt
        // die Diagnose-Instanz, falls die App (ohne Screenshot-Env) doch weiterlaeuft.
        // macOS hat kein GNU "timeout" - perl alarm als Ersatz
        var (_, consoleOut) = RunProcess(context, "bash",
        [
            "-c",
            $"perl -e 'alarm 40; exec @ARGV' xcrun simctl launch --console-pty {device.Udid} {context.ApplicationId} 2>&1 | tail -200"
        ], throwOnError: false);
        context.Information($"Console launch output:\n{consoleOut}");

        var (_, crash) = RunProcess(context, "bash",
        [
            "-c",
            "for dir in ~/Library/Logs/DiagnosticReports ~/Library/Logs/DiagnosticReports/Retired; do " +
            "echo \"--- $dir ---\"; ls -t \"$dir\" 2>/dev/null | head -8; " +
            "f=$(ls -t \"$dir\" 2>/dev/null | grep -i heimatplatz | head -1); " +
            "if [ -n \"$f\" ]; then echo \"--- $dir/$f ---\"; head -c 8000 \"$dir/$f\"; fi; done"
        ], throwOnError: false);
        context.Information($"Crash reports:\n{crash}");

        var (_, appLog) = RunProcess(context, "bash",
        [
            "-c",
            $"xcrun simctl spawn {device.Udid} log show --last 5m --style compact " +
            "--predicate 'process == \"Heimatplatz.Maui\" OR eventMessage CONTAINS[c] \"heimatplatz\"' 2>/dev/null | tail -150"
        ], throwOnError: false);
        context.Information($"Simulator log (Heimatplatz, last 5m):\n{appLog}");
    }

    private (int ExitCode, string Output) RunXcrun(
        BuildContext context,
        IReadOnlyList<string> arguments,
        IDictionary<string, string>? env = null,
        bool throwOnError = true)
        => RunProcess(context, "xcrun", arguments, env, throwOnError);

    private (int ExitCode, string Output) RunProcess(
        BuildContext context,
        string fileName,
        IReadOnlyList<string> arguments,
        IDictionary<string, string>? env = null,
        bool throwOnError = true)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            processInfo.ArgumentList.Add(argument);
        }
        if (env != null)
        {
            foreach (var (key, value) in env)
            {
                processInfo.Environment[key] = value;
            }
        }

        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName} process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var message = $"{fileName} {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {error}";
            if (throwOnError)
            {
                throw new InvalidOperationException(message);
            }
            context.Warning(message);
        }

        return (process.ExitCode, output + error);
    }

    private sealed record ScreenshotConfig(
        string Locale,
        string AppleLanguage,
        string AppleLocale,
        string? ApiBaseUrl,
        bool RunLocalTestApi,
        string TestDbConnectionString,
        string TestDbPassword,
        string LoginEmail,
        string LoginPassword,
        IReadOnlyList<string> Devices,
        IReadOnlyList<Shot> Shots);

    private sealed record Shot(string Name, string Route, int DelaySeconds);

    private sealed record SimulatorDevice(string Name, string Udid, string State, string Runtime);
}
