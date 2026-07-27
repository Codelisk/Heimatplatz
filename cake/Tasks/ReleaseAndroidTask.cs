using System.Diagnostics;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build.Tasks;

/// <summary>
/// Kompletter Play-Store-Release in einem Lauf (Wrapper: release-android.ps1):
///  1. VersionBump          - hoechster Store-Version-Code + 1 -> csproj
///  2. AndroidStoreTexts    - Claude CLI prueft/aktualisiert Texte (de/en) + Release-Notes
///  3. AndroidScreenshots   - deterministische Emulator-Screenshots mit Test-Login
///  4. BuildAndroid         - signiertes AAB nach artifacts/android
///  5. Play-Upload          - Listing, Bilder, Screenshots, AAB, Track-Release inkl. Notes
///  6. Git-Commit + Tag     - metadata + csproj committen, Tag android-v&lt;code&gt; (kein Push)
/// Die Schritte rufen die Einzel-Tasks direkt auf (kein Cake-Task-Graph), damit jeder
/// Einzel-Task weiterhin standalone lauffaehig bleibt, ohne einen Version-Bump auszuloesen.
/// Konfiguration: Sektion "Android:Release" in appsettings.json
/// (Track, ReleaseStatus, Locales, DefaultLocale, GitCommit, GitTag).
/// </summary>
[TaskName("ReleaseAndroid")]
public sealed class ReleaseAndroidTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Android Play Store Release ===");

        if (string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) || !File.Exists(context.PlayStoreJsonKeyPath))
        {
            throw new InvalidOperationException(
                $"Play Store JSON key not found: '{context.PlayStoreJsonKeyPath}'. " +
                "Configure Android:PlayStoreJsonKeyPath or PLAY_STORE_JSON_KEY_PATH.");
        }

        var section = context.Configuration.GetSection("Android:Release");
        var track = section["Track"] ?? "internal";
        var releaseStatus = section["ReleaseStatus"] ?? "completed";
        var locales = AndroidStoreTextsTask.GetLocales(context);
        var defaultLocale = section["DefaultLocale"] ?? locales[0];

        // 1. Version-Bump (Store-Baseline + 1 -> csproj)
        context.Information("--- Step 1/6: Version bump ---");
        new VersionBumpTask().Run(context);
        var (displayVersion, versionCode) = ReadVersion(context);
        context.Information($"Releasing v{displayVersion} (versionCode {versionCode}) to track '{track}'");

        // Der Kanal steckt fest im Binary (Play kann eine AAB nicht pro Track variieren).
        // Ein Test-Bundle darf deshalb NIE per Play-Promotion nach production wandern -
        // dort wuerde der API-Umschalter beim Endkunden landen.
        if (!track.Equals("production", StringComparison.OrdinalIgnoreCase))
        {
            context.Warning(
                $"Bundle wird mit Auslieferungskanal '{context.AppChannel}' gebaut (Entwicklerwerkzeuge aktiv). " +
                "Nicht in den Production-Track promoten - fuer Production mit -Track production neu bauen.");
        }

        // 2. Store-Texte via Claude CLI (inkl. Release-Notes fuer versionCode)
        context.Information("--- Step 2/6: Store texts (Claude CLI) ---");
        AndroidStoreTextsTask.RunCore(context, versionCode);

        // 3. Deterministische Emulator-Screenshots + Store-Grafiken aus den Markenassets
        context.Information("--- Step 3/6: Screenshots + Store-Grafiken ---");
        AndroidScreenshotsTask.RunCore(context);
        StoreArtTask.RunCore(context);

        // 4. AAB bauen (alte Artefakte vorher raeumen, damit kein altes Bundle hochgeht)
        context.Information("--- Step 4/6: Build AAB ---");
        var artifactsDir = Path.Combine(context.ProjectDirectory, "artifacts", "android");
        if (Directory.Exists(artifactsDir))
        {
            Directory.Delete(artifactsDir, recursive: true);
        }
        new BuildAndroidTask().Run(context);

        var aabFiles = Directory.GetFiles(artifactsDir, "*.aab", SearchOption.AllDirectories);
        var aabPath = aabFiles.FirstOrDefault(f => f.Contains("-Signed")) ?? aabFiles.FirstOrDefault()
            ?? throw new FileNotFoundException($"No AAB found in {artifactsDir}");
        context.Information($"AAB: {aabPath}");

        // 5. Alles in EINEM Play-Edit hochladen und committen
        context.Information("--- Step 5/6: Play Store upload ---");
        using (var client = new PlayStoreClient(context.PlayStoreJsonKeyPath, context.AndroidPackageName))
        {
            var editId = client.CreateEdit();
            context.Information($"Edit created: {editId}");

            PlayStoreUploader.UploadListings(context, client, editId, locales, defaultLocale);
            PlayStoreUploader.UploadImages(context, client, editId, locales, defaultLocale);
            PlayStoreUploader.UploadAppDetails(context, client, editId, defaultLocale);

            context.Information("Uploading AAB (this can take a while)...");
            var uploadedVersionCode = client.UploadBundle(editId, aabPath);
            if (uploadedVersionCode != versionCode)
            {
                context.Warning($"AAB contains versionCode {uploadedVersionCode}, expected {versionCode} - using the AAB value.");
                versionCode = uploadedVersionCode;
            }

            var releaseNotes = PlayStoreUploader.ReadReleaseNotes(context, locales, defaultLocale, versionCode);
            client.SetTrackRelease(editId, track, versionCode, releaseStatus, $"{displayVersion} ({versionCode})", releaseNotes);

            client.CommitEdit(editId);
        }
        context.Information($"Play Store release v{displayVersion} ({versionCode}) committed on track '{track}'.");

        // 6. Release im Repo festhalten
        context.Information("--- Step 6/6: Git commit + tag ---");
        CommitAndTag(context, section, displayVersion, versionCode, track);

        context.Information("=== Android release completed! ===");
    }

    private static (string DisplayVersion, int VersionCode) ReadVersion(BuildContext context)
    {
        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var display = doc.Descendants(ns + "ApplicationDisplayVersion").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("ApplicationDisplayVersion not found in csproj");
        var code = doc.Descendants(ns + "ApplicationVersion").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("ApplicationVersion not found in csproj");
        return (display, int.Parse(code));
    }

    private static void CommitAndTag(
        BuildContext context, IConfigurationSection section, string displayVersion, int versionCode, string track)
    {
        var commit = !bool.TryParse(section["GitCommit"], out var c) || c;
        var tag = !bool.TryParse(section["GitTag"], out var t) || t;
        if (!commit)
        {
            context.Information("Android:Release:GitCommit=false - skipping commit/tag.");
            return;
        }

        RunGit(context, ["add", context.CsprojPath, Path.Combine(context.FastlaneDirectory, "metadata", "android")]);

        var (statusExit, status) = RunGit(context, ["status", "--porcelain"], throwOnError: false);
        if (statusExit == 0 && string.IsNullOrWhiteSpace(status))
        {
            context.Information("Nothing to commit.");
        }
        else
        {
            RunGit(context,
            [
                "commit",
                "-m", $"release(android): v{displayVersion} ({versionCode}) - Play Store {track}",
                "--", context.CsprojPath, Path.Combine(context.FastlaneDirectory, "metadata", "android")
            ]);
            context.Information("Release committed.");
        }

        if (tag)
        {
            var tagName = $"android-v{versionCode}";
            var (tagExit, tagOutput) = RunGit(context, ["tag", tagName], throwOnError: false);
            if (tagExit == 0)
            {
                context.Information($"Tagged {tagName}. (Push mit: git push --follow-tags)");
            }
            else
            {
                context.Warning($"Tag {tagName} failed: {tagOutput.Trim()}");
            }
        }
    }

    private static (int ExitCode, string Output) RunGit(
        BuildContext context, IReadOnlyList<string> arguments, bool throwOnError = true)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = context.ProjectDirectory,
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
            ?? throw new InvalidOperationException("Failed to start git");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && throwOnError)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {error}");
        }
        return (process.ExitCode, output + error);
    }
}

/// <summary>
/// Diagnose: prueft Service-Account-Zugang und zeigt den hoechsten Version-Code
/// ueber alle Play-Tracks (Baseline des Version-Bumps). Aendert nichts (Edit wird
/// nie committet und verfaellt serverseitig).
/// </summary>
[TaskName("PlayStoreVersionCheck")]
public sealed class PlayStoreVersionCheckTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Play Store Version Check ===");
        using var client = new PlayStoreClient(context.PlayStoreJsonKeyPath, context.AndroidPackageName);
        var editId = client.CreateEdit();
        var highest = client.GetHighestVersionCode(editId);
        context.Information($"Package: {context.AndroidPackageName}");
        context.Information($"Highest version code across all tracks: {highest}");
        context.Information($"Next release would be versionCode {highest + 1}.");
    }
}
