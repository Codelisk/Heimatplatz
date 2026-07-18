using System.Diagnostics;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build.Tasks;

/// <summary>
/// Laesst die Claude CLI (Default-Modell, headless) die Play-Store-Texte pflegen:
/// prueft title/short_description/full_description in de-DE und en-US gegen den
/// aktuellen Funktionsumfang (Git-Log seit dem letzten android-v*-Tag wird in den
/// Prompt eingebettet), aktualisiert sie bei Bedarf und schreibt die Release-Notes
/// changelogs/&lt;versionCode&gt;.txt fuer beide Sprachen. Danach validiert der Task die
/// Play-Limits hart in C# (Titel 30, Kurzbeschreibung 80, Beschreibung 4000,
/// Release-Notes 500 Zeichen) - bei Verstoessen bricht der Build ab.
/// Konfiguration: Sektion "Android:StoreTexts" in appsettings.json.
/// </summary>
[TaskName("AndroidStoreTexts")]
public sealed class AndroidStoreTextsTask : FrostingTask<BuildContext>
{
    private const int TitleLimit = 30;
    private const int ShortDescriptionLimit = 80;
    private const int FullDescriptionLimit = 4000;
    private const int ChangelogLimit = 500;

    public override void Run(BuildContext context) => RunCore(context);

    /// <summary>Auch direkt vom ReleaseAndroid-Orchestrator aufrufbar (ohne Task-Graph).</summary>
    public static void RunCore(BuildContext context, int? versionCode = null)
    {
        context.Information("=== Android Store Texts (Claude CLI) ===");

        var locales = GetLocales(context);
        var targetVersionCode = versionCode ?? ResolveTargetVersionCode(context);
        var displayVersion = ReadDisplayVersion(context);
        var gitLog = GetGitLogSinceLastRelease(context);

        context.Information($"Target: v{displayVersion} (versionCode {targetVersionCode}), locales: {string.Join(", ", locales)}");

        RunClaude(context, BuildPrompt(context, locales, targetVersionCode, displayVersion, gitLog));
        Validate(context, locales, targetVersionCode);
        SyncIosReleaseNotes(context, targetVersionCode);

        context.Information("Store texts updated and validated.");
    }

    /// <summary>
    /// Spiegelt die frisch generierten de-DE-Release-Notes nach
    /// metadata/ios/de-DE/release_notes.txt - beide Stores bekommen denselben Text
    /// (SubmitIos liest die Datei als whatsNew). Das Play-Limit (500) ist strenger
    /// als das ASC-Limit (4000), der Text passt also immer.
    /// </summary>
    private static void SyncIosReleaseNotes(BuildContext context, int versionCode)
    {
        var androidChangelog = Path.Combine(
            context.FastlaneDirectory, "metadata", "android", "de-DE", "changelogs", $"{versionCode}.txt");
        var iosReleaseNotes = Path.Combine(
            context.FastlaneDirectory, "metadata", "ios", "de-DE", "release_notes.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(iosReleaseNotes)!);
        File.WriteAllText(iosReleaseNotes, File.ReadAllText(androidChangelog).Trim() + "\n");
        context.Information($"iOS-Release-Notes mit Android-Changelog {versionCode} synchronisiert.");
    }

    public static string[] GetLocales(BuildContext context)
    {
        var locales = context.Configuration.GetSection("Android:Release:Locales").GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();
        return locales.Length > 0 ? locales : ["de-DE", "en-US"];
    }

    /// <summary>
    /// Ziel-Version-Code: hoechster Play-Store-Code + 1 (gleiche Baseline wie VersionBump);
    /// ohne Store-Zugriff der Wert aus dem csproj.
    /// </summary>
    public static int ResolveTargetVersionCode(BuildContext context)
    {
        if (!string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) && File.Exists(context.PlayStoreJsonKeyPath))
        {
            var storeVersion = StoreVersionHelper.GetGooglePlayVersionCode(
                context.PlayStoreJsonKeyPath, context.AndroidPackageName, context.FastlaneDirectory);
            if (storeVersion.HasValue)
            {
                return storeVersion.Value + 1;
            }
            context.Warning("Google Play version query failed - falling back to csproj ApplicationVersion.");
        }

        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var value = doc.Descendants(ns + "ApplicationVersion").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("ApplicationVersion not found in csproj");
        return int.Parse(value);
    }

    private static string ReadDisplayVersion(BuildContext context)
    {
        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        return doc.Descendants(ns + "ApplicationDisplayVersion").FirstOrDefault()?.Value ?? "?";
    }

    /// <summary>Git-Log seit dem letzten android-v*-Tag (sonst die letzten 60 Commits), gedeckelt.</summary>
    private static string GetGitLogSinceLastRelease(BuildContext context)
    {
        var (tagExit, tagOutput) = RunGit(context, "tag --list android-v* --sort=-v:refname");
        var lastTag = tagExit == 0
            ? tagOutput.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0)
            : null;

        var range = lastTag != null ? $"{lastTag}..HEAD" : "-n 60";
        var (logExit, logOutput) = RunGit(context, $"log --oneline --no-merges {range}");
        if (logExit != 0)
        {
            context.Warning("git log failed - Claude gets no commit context.");
            return "(kein Git-Log verfuegbar)";
        }

        var lines = logOutput.Split('\n').Where(l => l.Trim().Length > 0).ToList();
        var header = lastTag != null
            ? $"Commits seit dem letzten Android-Release ({lastTag}):"
            : "Letzte Commits (noch kein android-v*-Release-Tag vorhanden):";

        var capped = lines.Take(100).ToList();
        var suffix = lines.Count > capped.Count ? $"\n... ({lines.Count - capped.Count} weitere Commits gekuerzt)" : string.Empty;
        return $"{header}\n{string.Join('\n', capped)}{suffix}";
    }

    private static string BuildPrompt(
        BuildContext context, string[] locales, int versionCode, string displayVersion, string gitLog)
    {
        var metadataRoot = Path.Combine("cake", "fastlane", "metadata", "android").Replace('\\', '/');
        var localeList = string.Join(" und ", locales);

        return $"""
            Du pflegst den Google-Play-Store-Eintrag der Immobilien-App "Heimatplatz"
            (Immobilien, Grundstuecke und Zwangsversteigerungen in Oberoesterreich).
            Es steht das Release v{displayVersion} (versionCode {versionCode}) an.

            Die Store-Texte liegen unter {metadataRoot}/<locale>/ fuer die Locales {localeList}:
            - title.txt (max. {TitleLimit} Zeichen)
            - short_description.txt (max. {ShortDescriptionLimit} Zeichen)
            - full_description.txt (max. {FullDescriptionLimit} Zeichen)
            - changelogs/<versionCode>.txt (max. {ChangelogLimit} Zeichen)

            Deine Aufgaben:
            1. Verschaffe dir einen Ueberblick ueber den aktuellen Funktionsumfang der App
               (MAUI-App unter src/maui/src/Heimatplatz.Maui, insbesondere Features/ und die
               Shell-Routen) und lies die bestehenden Texte in {metadataRoot}/.
            2. Pruefe title.txt, short_description.txt und full_description.txt in ALLEN Locales:
               Beschreiben sie die App noch korrekt und vollstaendig? Fehlen neue Features,
               werden entfernte Features beworben? Aktualisiere NUR, was nicht mehr passt -
               Stil, Struktur und Tonalitaet der bestehenden deutschen Texte beibehalten.
               de-DE ist die Quellsprache; en-US ist deren sinngemaesse Uebersetzung mit
               identischer Struktur. Fehlende en-US-Dateien legst du an.
            3. Schreibe die Release-Notes fuer dieses Release nach
               {metadataRoot}/<locale>/changelogs/{versionCode}.txt (fuer JEDE Locale):
               kurze Aufzaehlung (Zeilen mit "• ") der NUTZERSICHTBAREN Aenderungen aus dem
               Git-Log unten. Interne Umbauten, CI/Build- und Reine-Code-Aenderungen weglassen.
               WICHTIG: Der deutsche Text wird 1:1 auch als App-Store-Release-Notes (iOS)
               verwendet - formuliere store-neutral (kein "Play Store", kein "Android",
               keine plattformspezifischen Features erwaehnen, die es auf iOS nicht gibt).
               Gibt es nichts Nutzersichtbares, schreibe einen kurzen generischen Text
               ("• Fehlerbehebungen und Verbesserungen" bzw. "• Bug fixes and improvements").
            4. Halte die Zeichenlimits strikt ein (werden nach deinem Lauf hart validiert).

            Aendere ausschliesslich Dateien unter {metadataRoot}/. Keine Rueckfragen -
            triff sinnvolle Entscheidungen selbst.

            {gitLog}
            """;
    }

    private static void RunClaude(BuildContext context, string prompt)
    {
        var model = context.Configuration["Android:StoreTexts:ClaudeModel"];

        var processInfo = new ProcessStartInfo
        {
            FileName = "claude",
            WorkingDirectory = context.ProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processInfo.ArgumentList.Add("-p");
        processInfo.ArgumentList.Add(prompt);
        processInfo.ArgumentList.Add("--permission-mode");
        processInfo.ArgumentList.Add("acceptEdits");
        processInfo.ArgumentList.Add("--allowedTools");
        processInfo.ArgumentList.Add("Read,Glob,Grep,Edit,Write,Bash(git log:*),Bash(git diff:*),Bash(git show:*)");
        if (!string.IsNullOrWhiteSpace(model))
        {
            processInfo.ArgumentList.Add("--model");
            processInfo.ArgumentList.Add(model);
        }

        context.Information("Running Claude CLI (default model, headless)...");
        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start claude CLI - is it installed and on PATH?");

        process.OutputDataReceived += (_, e) => { if (e.Data != null) context.Information($"[claude] {e.Data}"); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) context.Warning($"[claude] {e.Data}"); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(TimeSpan.FromMinutes(20)))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Claude CLI did not finish within 20 minutes");
        }
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Claude CLI failed with exit code {process.ExitCode}");
        }
    }

    private static void Validate(BuildContext context, string[] locales, int versionCode)
    {
        var violations = new List<string>();

        foreach (var locale in locales)
        {
            var localeDir = Path.Combine(context.FastlaneDirectory, "metadata", "android", locale);

            CheckFile(violations, Path.Combine(localeDir, "title.txt"), TitleLimit, required: true);
            CheckFile(violations, Path.Combine(localeDir, "short_description.txt"), ShortDescriptionLimit, required: true);
            CheckFile(violations, Path.Combine(localeDir, "full_description.txt"), FullDescriptionLimit, required: true);
            CheckFile(violations, Path.Combine(localeDir, "changelogs", $"{versionCode}.txt"), ChangelogLimit, required: true);
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Store text validation failed:\n  - " + string.Join("\n  - ", violations));
        }

        foreach (var locale in locales)
        {
            var changelog = Path.Combine(context.FastlaneDirectory, "metadata", "android", locale, "changelogs", $"{versionCode}.txt");
            context.Information($"[{locale}] release notes:\n{File.ReadAllText(changelog).Trim()}");
        }
    }

    private static void CheckFile(List<string> violations, string path, int limit, bool required)
    {
        if (!File.Exists(path))
        {
            if (required)
                violations.Add($"{path}: fehlt");
            return;
        }

        var content = File.ReadAllText(path).Trim();
        if (content.Length == 0)
        {
            violations.Add($"{path}: leer");
        }
        else if (content.Length > limit)
        {
            violations.Add($"{path}: {content.Length} Zeichen (Limit {limit})");
        }
    }

    private static (int ExitCode, string Output) RunGit(BuildContext context, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = context.ProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start git");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output + error);
    }
}
