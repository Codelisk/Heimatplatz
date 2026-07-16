using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build.Tasks;

/// <summary>
/// Erzeugt deterministische Play-Store-Screenshots im Android-Emulator (Windows-faehig):
/// Release-APK-Build (debug-signiert, reicht fuer den Emulator), Boot des konfigurierten
/// AVD, Demo-Statusbar (09:41, WLAN voll, Akku 100%), frische Installation, App-Start pro
/// konfigurierter Shell-Route im Screenshot-Modus mit Auto-Login. Die Steuerung laeuft
/// ueber "adb shell setprop debug.heimatplatz.*" (ohne Root erlaubt); die App uebersetzt
/// die Sysprops im Emulator in Env-Vars (ScreenshotSysProps) fuer den geteilten
/// ScreenshotMode. Standard-API ist die gehostete Test-API (test-api.heimatplatz.at)
/// mit geseedeten Daten - kein lokaler API-Start noetig.
/// Ausgabe: cake/fastlane/metadata/android/&lt;locale&gt;/images/&lt;imageType&gt;/&lt;shot&gt;.png
/// (direkt im fastlane-kompatiblen Metadata-Layout, damit Upload und Git-Diff zusammenfallen).
/// Konfiguration: Sektion "Android:Screenshots" in appsettings.json.
/// </summary>
[TaskName("AndroidScreenshots")]
public sealed class AndroidScreenshotsTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => RunCore(context);

    /// <summary>Auch direkt vom ReleaseAndroid-Orchestrator aufrufbar (ohne Task-Graph).</summary>
    public static void RunCore(BuildContext context)
    {
        context.Information("=== Android Play Store Screenshots ===");

        var config = LoadConfig(context);
        var adb = ResolveSdkTool(context, "platform-tools", "adb");
        var emulatorExe = ResolveSdkTool(context, "emulator", "emulator");

        var apkPath = BuildEmulatorApk(context, config);

        foreach (var device in config.Devices)
        {
            var outputDir = Path.Combine(
                context.FastlaneDirectory, "metadata", "android", config.Locale, "images", device.ImageType);
            Directory.CreateDirectory(outputDir);

            CaptureDevice(context, config, device, adb, emulatorExe, apkPath, outputDir);
        }
    }

    private static ScreenshotConfig LoadConfig(BuildContext context)
    {
        var section = context.Configuration.GetSection("Android:Screenshots");

        var devices = section.GetSection("Devices").GetChildren()
            .Select(c => new Device(
                c["Avd"] ?? throw new InvalidOperationException("Android:Screenshots:Devices entry without Avd"),
                c["ImageType"] ?? "phoneScreenshots"))
            .ToList();
        if (devices.Count == 0)
        {
            devices = [new Device("pixel_5_-_api_30", "phoneScreenshots")];
        }

        var defaultDelay = int.TryParse(section["LaunchDelaySeconds"], out var d) ? d : 12;

        var shots = section.GetSection("Shots").GetChildren()
            .Select(c => new Shot(
                c["Name"] ?? throw new InvalidOperationException("Android:Screenshots:Shots entry without Name"),
                c["Route"] ?? string.Empty,
                int.TryParse(c["DelaySeconds"], out var sd) ? sd : defaultDelay))
            .ToList();
        if (shots.Count == 0)
        {
            shots = [new Shot("01_startseite", string.Empty, defaultDelay)];
        }

        return new ScreenshotConfig(
            Locale: section["Locale"] ?? "de-DE",
            ApiBaseUrl: section["ApiBaseUrl"] ?? "https://test-api.heimatplatz.at",
            RuntimeIdentifier: section["RuntimeIdentifier"] ?? "android-x64",
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

    /// <summary>ANDROID_HOME/ANDROID_SDK_ROOT -> bekannte Pfade -> PATH-Fallback.</summary>
    private static string ResolveSdkTool(BuildContext context, string sdkSubdir, string toolName)
    {
        var exe = OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;

        var sdkRoots = new[]
        {
            context.Configuration["Android:SdkPath"],
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
            @"C:\Program Files (x86)\Android\android-sdk"
        };

        foreach (var root in sdkRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            var candidate = Path.Combine(root, sdkSubdir, exe);
            if (File.Exists(candidate))
                return candidate;
        }

        // PATH-Fallback: Prozessstart schlaegt sonst mit klarer Meldung fehl
        context.Warning($"{toolName} not found under known SDK roots - relying on PATH.");
        return toolName;
    }

    private static string BuildEmulatorApk(BuildContext context, ScreenshotConfig config)
    {
        context.Information($"Building emulator APK (Release, {config.RuntimeIdentifier})...");

        var restoreSettings = new DotNetRestoreSettings();
        restoreSettings.ConfigFile = Path.Combine(context.ProjectDirectory, "nuget.config");
        context.DotNetRestore(context.CsprojPath, restoreSettings);

        var outputDir = Path.Combine(context.ProjectDirectory, "artifacts", "screenshots", "android-apk");
        Directory.CreateDirectory(outputDir);

        var msBuildSettings = new Cake.Common.Tools.DotNet.MSBuild.DotNetMSBuildSettings();
        msBuildSettings.Properties["AndroidPackageFormat"] = new[] { "apk" };
        // Emulator-Image-Architektur; das csproj uebersetzt die Property in
        // RuntimeIdentifiers (globales RuntimeIdentifiers wuerde in ProjectReferences
        // lecken -> MSB3030 im ApiClient). Ohne Keystore signiert .NET mit dem
        // Debug-Key - fuer die Emulator-Installation ausreichend
        msBuildSettings.Properties["ScreenshotRuntimeIdentifier"] = new[] { config.RuntimeIdentifier };

        context.DotNetPublish(context.CsprojPath, new DotNetPublishSettings
        {
            Configuration = "Release",
            Framework = "net10.0-android",
            OutputDirectory = outputDir,
            MSBuildSettings = msBuildSettings
        });

        var apk = Directory.GetFiles(outputDir, "*-Signed.apk").FirstOrDefault()
            ?? Directory.GetFiles(outputDir, "*.apk").FirstOrDefault()
            ?? throw new InvalidOperationException($"No APK found under {outputDir}");

        context.Information($"APK: {apk}");
        return apk;
    }

    private static void CaptureDevice(
        BuildContext context,
        ScreenshotConfig config,
        Device device,
        string adb,
        string emulatorExe,
        string apkPath,
        string outputDir)
    {
        var (serial, startedByUs, emulatorProcess) = EnsureEmulator(context, config, device, adb, emulatorExe);
        context.Information($"[{device.Avd}] using {serial} (started by task: {startedByUs})");

        string[] Adb(params string[] args) => ["-s", serial, .. args];

        try
        {
            // Bildschirm an, Keyguard weg, nicht einschlafen lassen
            Run(context, adb, Adb("shell", "svc", "power", "stayon", "true"), throwOnError: false);
            Run(context, adb, Adb("shell", "wm", "dismiss-keyguard"), throwOnError: false);
            Run(context, adb, Adb("shell", "input", "keyevent", "KEYCODE_WAKEUP"), throwOnError: false);

            // Deterministische Status-Bar via SystemUI-Demo-Mode: 09:41, WLAN voll,
            // Akku 100%, keine Notifications, kein Mobilfunk
            Run(context, adb, Adb("shell", "settings", "put", "global", "sysui_demo_allowed", "1"));
            Demo(context, adb, serial, "enter");
            Demo(context, adb, serial, "clock", "-e", "hhmm", "0941");
            Demo(context, adb, serial, "network", "-e", "wifi", "show", "-e", "level", "4", "-e", "fully", "true");
            Demo(context, adb, serial, "network", "-e", "mobile", "hide");
            Demo(context, adb, serial, "battery", "-e", "level", "100", "-e", "plugged", "false");
            Demo(context, adb, serial, "notifications", "-e", "visible", "false");

            // Frische Installation fuer reproduzierbaren App-Zustand
            Run(context, adb, Adb("uninstall", context.ApplicationId), throwOnError: false);
            Run(context, adb, Adb("install", "-r", apkPath));

            // Screenshot-Steuerung: debug.*-Sysprops sind fuer die adb-Shell ohne Root
            // setzbar; ScreenshotSysProps in der App uebersetzt sie in Env-Vars
            SetProp(context, adb, serial, "debug.heimatplatz.screenshot", "1");
            SetProp(context, adb, serial, "debug.heimatplatz.apiurl", config.ApiBaseUrl);
            SetProp(context, adb, serial, "debug.heimatplatz.email", config.LoginEmail);
            SetProp(context, adb, serial, "debug.heimatplatz.password", config.LoginPassword);

            var component = ResolveLaunchComponent(context, adb, serial, context.ApplicationId);

            // Stale PNGs entfernen, damit geloeschte Shots nicht liegen bleiben
            foreach (var stale in Directory.GetFiles(outputDir, "*.png"))
            {
                File.Delete(stale);
            }

            foreach (var shot in config.Shots)
            {
                context.Information($"[{device.Avd}] {shot.Name} (Route: '{shot.Route}', {shot.DelaySeconds}s)...");

                Run(context, adb, Adb("shell", "am", "force-stop", context.ApplicationId), throwOnError: false);
                SetProp(context, adb, serial, "debug.heimatplatz.route", shot.Route);

                if (component != null)
                {
                    Run(context, adb, Adb("shell", "am", "start", "-W", "-n", component));
                }
                else
                {
                    // Fallback ohne Komponentennamen (crc64-Mangling): monkey startet die
                    // Launcher-Activity des Pakets
                    Run(context, adb, Adb("shell", "monkey", "-p", context.ApplicationId,
                        "-c", "android.intent.category.LAUNCHER", "1"));
                }

                Thread.Sleep(TimeSpan.FromSeconds(shot.DelaySeconds));

                var file = Path.Combine(outputDir, $"{shot.Name}.png");
                CaptureSettledScreenshot(context, adb, serial, file);

                // Liveness-Check: laeuft der Prozess nicht mehr, zeigt das PNG nur den Launcher
                var (_, pid) = Run(context, adb, Adb("shell", "pidof", context.ApplicationId), throwOnError: false);
                if (string.IsNullOrWhiteSpace(pid))
                {
                    DumpDiagnostics(context, adb, serial);
                    throw new InvalidOperationException(
                        $"App was no longer running when shot '{shot.Name}' was captured - startup crash? (see logcat above)");
                }

                context.Information($"  -> {file}");
            }
        }
        finally
        {
            // ScreenshotMode-Statuszeilen (Login/Navigation) sichtbar machen
            var (_, log) = Run(context, adb, Adb("logcat", "-d"), throwOnError: false, logOutput: false);
            var modeLines = log.Split('\n').Where(l => l.Contains("[ScreenshotMode]")).TakeLast(30);
            context.Information($"[ScreenshotMode] app log ({device.Avd}):\n{string.Join('\n', modeLines)}");

            Run(context, adb, Adb("shell", "am", "force-stop", context.ApplicationId), throwOnError: false);
            SetProp(context, adb, serial, "debug.heimatplatz.screenshot", "");
            SetProp(context, adb, serial, "debug.heimatplatz.route", "");
            SetProp(context, adb, serial, "debug.heimatplatz.email", "");
            SetProp(context, adb, serial, "debug.heimatplatz.password", "");
            SetProp(context, adb, serial, "debug.heimatplatz.apiurl", "");
            Demo(context, adb, serial, "exit");
            Run(context, adb, Adb("shell", "svc", "power", "stayon", "false"), throwOnError: false);

            if (startedByUs)
            {
                context.Information($"Shutting down emulator {serial}...");
                Run(context, adb, Adb("emu", "kill"), throwOnError: false);
                emulatorProcess?.WaitForExit(30_000);
            }
            emulatorProcess?.Dispose();
        }
    }

    /// <summary>
    /// Findet einen bereits laufenden Emulator mit dem konfigurierten AVD-Namen oder
    /// bootet ihn frisch (Snapshot-Load erlaubt - Determinismus kommt aus Demo-Statusbar
    /// und frischer App-Installation, nicht aus dem Kaltstart).
    /// </summary>
    private static (string Serial, bool StartedByUs, Process? EmulatorProcess) EnsureEmulator(
        BuildContext context, ScreenshotConfig config, Device device, string adb, string emulatorExe)
    {
        var existing = FindRunningEmulator(context, adb, device.Avd);
        if (existing != null)
        {
            return (existing, false, null);
        }

        context.Information($"Booting emulator '{device.Avd}'...");
        var before = ListEmulatorSerials(context, adb);

        var processInfo = new ProcessStartInfo
        {
            FileName = emulatorExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        processInfo.ArgumentList.Add("-avd");
        processInfo.ArgumentList.Add(device.Avd);
        processInfo.ArgumentList.Add("-no-boot-anim");
        processInfo.ArgumentList.Add("-no-audio");

        var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start emulator process");
        process.OutputDataReceived += (_, e) => { if (e.Data != null) context.Debug($"[emulator] {e.Data}"); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) context.Debug($"[emulator] {e.Data}"); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Auf den neuen Serial warten, dann auf sys.boot_completed=1
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(6);
        string? serial = null;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Emulator exited prematurely with code {process.ExitCode}");
            }

            serial ??= ListEmulatorSerials(context, adb).Except(before).FirstOrDefault();
            if (serial != null)
            {
                var (exit, output) = Run(context, adb,
                    ["-s", serial, "shell", "getprop", "sys.boot_completed"],
                    throwOnError: false, logOutput: false);
                if (exit == 0 && output.Trim() == "1")
                {
                    context.Information($"Emulator booted: {serial}");
                    // SystemUI kurz atmen lassen, sonst ignoriert der Demo-Mode Broadcasts
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    return (serial, true, process);
                }
            }
            Thread.Sleep(2000);
        }

        process.Kill(entireProcessTree: true);
        throw new InvalidOperationException($"Emulator '{device.Avd}' did not boot within 6 minutes");
    }

    private static string? FindRunningEmulator(BuildContext context, string adb, string avdName)
    {
        foreach (var serial in ListEmulatorSerials(context, adb))
        {
            var (exit, output) = Run(context, adb, ["-s", serial, "emu", "avd", "name"],
                throwOnError: false, logOutput: false);
            if (exit != 0)
                continue;
            var name = output.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && l != "OK");
            if (string.Equals(name, avdName, StringComparison.OrdinalIgnoreCase))
                return serial;
        }
        return null;
    }

    private static IReadOnlyList<string> ListEmulatorSerials(BuildContext context, string adb)
    {
        var (_, output) = Run(context, adb, ["devices"], throwOnError: false, logOutput: false);
        return output.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("emulator-") && l.EndsWith("device"))
            .Select(l => l.Split('\t', ' ')[0])
            .ToList();
    }

    private static string? ResolveLaunchComponent(BuildContext context, string adb, string serial, string packageName)
    {
        var (exit, output) = Run(context, adb,
            ["-s", serial, "shell", "cmd", "package", "resolve-activity", "--brief", packageName],
            throwOnError: false, logOutput: false);
        if (exit != 0)
            return null;

        var component = output.Split('\n')
            .Select(l => l.Trim())
            .LastOrDefault(l => l.Contains('/'));
        context.Information($"Launcher component: {component ?? "(monkey fallback)"}");
        return component;
    }

    private static void SetProp(BuildContext context, string adb, string serial, string name, string value)
    {
        // Immer single-quoten: die Geraete-Shell interpretiert die zusammengefuegten
        // adb-shell-Argumente erneut (Leerzeichen/!-Zeichen); leerer Wert loescht die
        // Property effektiv (App ignoriert leere Werte)
        Run(context, adb, ["-s", serial, "shell", "setprop", name, $"'{value.Replace("'", "'\\''")}'"]);
    }

    private static void Demo(BuildContext context, string adb, string serial, string command, params string[] extraArgs)
    {
        Run(context, adb,
            ["-s", serial, "shell", "am", "broadcast", "-a", "com.android.systemui.demo", "-e", "command", command, .. extraArgs],
            throwOnError: false, logOutput: false);
    }

    /// <summary>
    /// Screenshottet, bis zwei aufeinanderfolgende Aufnahmen identisch sind (gleiche Logik
    /// wie IosScreenshots): Spinner/Animationen erzeugen Pixel-Differenzen, die fertige
    /// Seite mit eingefrorener Demo-Statusbar liefert ein stabiles Bild.
    /// </summary>
    private static void CaptureSettledScreenshot(BuildContext context, string adb, string serial, string file)
    {
        const int maxAttempts = 12;
        const int settleIntervalSeconds = 3;
        const string devicePath = "/data/local/tmp/hp_screenshot.png";

        void Capture()
        {
            Run(context, adb, ["-s", serial, "shell", "screencap", "-p", devicePath]);
            Run(context, adb, ["-s", serial, "pull", devicePath, file], logOutput: false);
        }

        Capture();
        var previousHash = HashFile(file);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(settleIntervalSeconds));
            Capture();

            var hash = HashFile(file);
            if (hash == previousHash)
            {
                context.Information($"Screen settled after {attempt} check(s).");
                Run(context, adb, ["-s", serial, "shell", "rm", "-f", devicePath], throwOnError: false, logOutput: false);
                return;
            }
            previousHash = hash;
        }

        context.Warning(
            $"Screen did not settle within {maxAttempts * settleIntervalSeconds}s - " +
            "keeping last capture (page may still be loading/animating).");
    }

    private static string HashFile(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    private static void DumpDiagnostics(BuildContext context, string adb, string serial)
    {
        context.Warning("Collecting logcat diagnostics...");
        var (_, log) = Run(context, adb, ["-s", serial, "logcat", "-d", "-t", "400"],
            throwOnError: false, logOutput: false);
        var relevant = log.Split('\n')
            .Where(l => l.Contains("heimatplatz", StringComparison.OrdinalIgnoreCase)
                || l.Contains("ScreenshotMode")
                || l.Contains("FATAL", StringComparison.OrdinalIgnoreCase)
                || l.Contains("AndroidRuntime"))
            .TakeLast(150);
        context.Information($"logcat (relevant):\n{string.Join('\n', relevant)}");
    }

    private static (int ExitCode, string Output) Run(
        BuildContext context,
        string fileName,
        IReadOnlyList<string> arguments,
        bool throwOnError = true,
        bool logOutput = true)
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

        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (logOutput && !string.IsNullOrWhiteSpace(output))
        {
            context.Debug(output.Trim());
        }

        if (process.ExitCode != 0)
        {
            var message = $"{Path.GetFileName(fileName)} {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {error}";
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
        string ApiBaseUrl,
        string RuntimeIdentifier,
        string LoginEmail,
        string LoginPassword,
        IReadOnlyList<Device> Devices,
        IReadOnlyList<Shot> Shots);

    private sealed record Device(string Avd, string ImageType);

    private sealed record Shot(string Name, string Route, int DelaySeconds);
}
