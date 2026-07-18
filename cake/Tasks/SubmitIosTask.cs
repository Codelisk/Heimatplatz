using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Reicht die aktuelle iOS-Version zum App-Store-Review ein - komplett ueber die
/// App-Store-Connect-API (laeuft damit auch unter Windows, kein fastlane/macOS noetig):
///  1. Neuesten verarbeiteten TestFlight-Build ermitteln
///  2. Editierbare App-Store-Version finden oder anlegen (Versions-String = {major}.{build}.0,
///     Auto-Release nach Freigabe)
///  3. Export-Compliance am Build sicherstellen (Fallback; Info.plist deklariert sie bereits)
///  4. de-DE-Metadaten aus cake/fastlane/metadata/ios/de-DE setzen
///     (release_notes.txt -> whatsNew, promotional_text.txt -> promotionalText)
///  5. Build anhaengen, Review-Submission anlegen und einreichen
/// Aufruf: ./build.ps1 -Target SubmitIos (bewusst KEIN Bestandteil von DeployIos -
/// der Submit bleibt ein expliziter Schritt).
/// </summary>
[TaskName("SubmitIos")]
public sealed class SubmitIosTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Submit iOS zur App-Store-Review (ASC-API) ===");

        if (string.IsNullOrEmpty(context.AppStoreConnectApiKeyId) ||
            string.IsNullOrEmpty(context.AppStoreConnectKeyPath) ||
            !File.Exists(context.AppStoreConnectKeyPath))
        {
            throw new InvalidOperationException(
                "App-Store-Connect-Credentials fehlen (iOS:AppStoreConnect* in appsettings.Local.json bzw. ASC_*-Env-Vars).");
        }

        using var asc = new AscClient(
            context.AppStoreConnectApiKeyId,
            context.AppStoreConnectIssuerId,
            context.AppStoreConnectKeyPath);

        // App aufloesen
        var apps = asc.Get($"v1/apps?filter[bundleId]={Uri.EscapeDataString(context.IosBundleId)}&limit=1");
        if (apps.RootElement.GetProperty("data").GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"Keine App fuer BundleId {context.IosBundleId} gefunden.");
        }
        var appId = apps.RootElement.GetProperty("data")[0].GetProperty("id").GetString()!;

        // Neuesten verarbeiteten Build ermitteln. Mit SUBMIT_MIN_BUILD wartet der Task,
        // bis Apple einen Build >= dieser Nummer fertig verarbeitet hat (VALID) - noetig
        // im Gesamtlauf (release-all.ps1): direkt nach dem Codemagic-Build ist der neue
        // Build noch PROCESSING und der Task wuerde sonst den VORHERIGEN einreichen.
        var minBuildEnv = Environment.GetEnvironmentVariable("SUBMIT_MIN_BUILD");
        var minBuild = int.TryParse(minBuildEnv, out var parsedMin) ? parsedMin : 0;
        if (minBuild > 0)
        {
            context.Information($"Warte auf verarbeiteten TestFlight-Build >= {minBuild} (max. 45 min)...");
        }

        string? buildId = null;
        var buildNumber = 0;
        var deadline = DateTime.UtcNow.AddMinutes(45);
        while (true)
        {
            var builds = asc.Get($"v1/builds?filter[app]={appId}&sort=-uploadedDate&limit=5");
            foreach (var build in builds.RootElement.GetProperty("data").EnumerateArray())
            {
                var attrs = build.GetProperty("attributes");
                if (attrs.GetProperty("processingState").GetString() != "VALID")
                    continue;
                var candidateNumber = int.Parse(attrs.GetProperty("version").GetString()!);
                if (candidateNumber < minBuild)
                    continue;
                buildId = build.GetProperty("id").GetString();
                buildNumber = candidateNumber;
                break;
            }

            if (buildId is not null || minBuild == 0 || DateTime.UtcNow >= deadline)
                break;

            Thread.Sleep(60_000);
        }

        if (buildId is null)
        {
            throw new InvalidOperationException(minBuild > 0
                ? $"Kein verarbeiteter (VALID) TestFlight-Build >= {minBuild} innerhalb des Zeitlimits gefunden."
                : "Kein verarbeiteter (VALID) TestFlight-Build gefunden - erst DeployIos laufen lassen.");
        }

        // Versions-String nach VersionBump-Schema {major}.{build}.0
        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var displayVersion = doc.Descendants(ns + "ApplicationDisplayVersion").First().Value;
        var major = displayVersion.Split('.')[0];
        var versionString = $"{major}.{buildNumber}.0";
        context.Information($"Ziel: Version {versionString} mit Build {buildNumber} ({buildId})");

        // Editierbare Version finden oder anlegen
        string[] editableStates =
        [
            "PREPARE_FOR_SUBMISSION", "DEVELOPER_REJECTED", "REJECTED",
            "METADATA_REJECTED", "INVALID_BINARY"
        ];
        string[] blockingStates = ["WAITING_FOR_REVIEW", "IN_REVIEW", "PENDING_DEVELOPER_RELEASE"];

        var versions = asc.Get($"v1/apps/{appId}/appStoreVersions?limit=10");
        string? versionId = null;
        foreach (var version in versions.RootElement.GetProperty("data").EnumerateArray())
        {
            var state = version.GetProperty("attributes").GetProperty("appStoreState").GetString()!;
            if (blockingStates.Contains(state))
            {
                throw new InvalidOperationException(
                    $"Version {version.GetProperty("attributes").GetProperty("versionString").GetString()} " +
                    $"ist bereits im Status {state} - zuerst freigeben lassen oder die Submission zurueckziehen.");
            }
            if (editableStates.Contains(state))
            {
                versionId = version.GetProperty("id").GetString();
                var currentString = version.GetProperty("attributes").GetProperty("versionString").GetString();
                if (currentString != versionString)
                {
                    context.Information($"Passe Versions-String {currentString} -> {versionString} an...");
                    asc.Patch($"v1/appStoreVersions/{versionId}", new
                    {
                        data = new
                        {
                            type = "appStoreVersions",
                            id = versionId,
                            attributes = new { versionString }
                        }
                    });
                }
                break;
            }
        }

        if (versionId is null)
        {
            context.Information($"Lege neue App-Store-Version {versionString} an (Auto-Release nach Freigabe)...");
            var created = asc.Post("v1/appStoreVersions", new
            {
                data = new
                {
                    type = "appStoreVersions",
                    attributes = new { platform = "IOS", versionString, releaseType = "AFTER_APPROVAL" },
                    relationships = new { app = new { data = new { type = "apps", id = appId } } }
                }
            });
            versionId = created.RootElement.GetProperty("data").GetProperty("id").GetString()!;
        }

        // Export-Compliance (Fallback fuer Builds ohne ITSAppUsesNonExemptEncryption im Info.plist)
        try
        {
            asc.Patch($"v1/builds/{buildId}", new
            {
                data = new
                {
                    type = "builds",
                    id = buildId,
                    attributes = new { usesNonExemptEncryption = false }
                }
            });
        }
        catch (Exception ex)
        {
            context.Warning($"Compliance-PATCH uebersprungen ({ex.Message}) - vermutlich bereits per Info.plist deklariert.");
        }

        // Metadaten aus dem Repo setzen (aktuell nur de-DE gepflegt)
        var metadataDir = Path.Combine(context.FastlaneDirectory, "metadata", "ios");
        var localizations = asc.Get($"v1/appStoreVersions/{versionId}/appStoreVersionLocalizations");
        foreach (var localization in localizations.RootElement.GetProperty("data").EnumerateArray())
        {
            var locale = localization.GetProperty("attributes").GetProperty("locale").GetString()!;
            var localeDir = Path.Combine(metadataDir, locale);
            if (!Directory.Exists(localeDir))
                continue;

            var attributes = new Dictionary<string, string>();
            var releaseNotesPath = Path.Combine(localeDir, "release_notes.txt");
            if (File.Exists(releaseNotesPath))
                attributes["whatsNew"] = File.ReadAllText(releaseNotesPath, Encoding.UTF8).Trim();
            var promoPath = Path.Combine(localeDir, "promotional_text.txt");
            if (File.Exists(promoPath))
                attributes["promotionalText"] = File.ReadAllText(promoPath, Encoding.UTF8).Trim();

            if (attributes.Count == 0)
                continue;

            context.Information($"[{locale}] setze {string.Join(", ", attributes.Keys)}...");
            asc.Patch($"v1/appStoreVersionLocalizations/{localization.GetProperty("id").GetString()}", new
            {
                data = new
                {
                    type = "appStoreVersionLocalizations",
                    id = localization.GetProperty("id").GetString(),
                    attributes
                }
            });
        }

        // Build anhaengen
        context.Information("Haenge Build an die Version an...");
        asc.Patch($"v1/appStoreVersions/{versionId}/relationships/build",
            new { data = new { type = "builds", id = buildId } });

        // Einreichen
        context.Information("Reiche zur Review ein...");
        var submission = asc.Post("v1/reviewSubmissions", new
        {
            data = new
            {
                type = "reviewSubmissions",
                attributes = new { platform = "IOS" },
                relationships = new { app = new { data = new { type = "apps", id = appId } } }
            }
        });
        var submissionId = submission.RootElement.GetProperty("data").GetProperty("id").GetString()!;

        asc.Post("v1/reviewSubmissionItems", new
        {
            data = new
            {
                type = "reviewSubmissionItems",
                relationships = new
                {
                    reviewSubmission = new { data = new { type = "reviewSubmissions", id = submissionId } },
                    appStoreVersion = new { data = new { type = "appStoreVersions", id = versionId } }
                }
            }
        });

        asc.Patch($"v1/reviewSubmissions/{submissionId}", new
        {
            data = new
            {
                type = "reviewSubmissions",
                id = submissionId,
                attributes = new { submitted = true }
            }
        });

        context.Information($"Version {versionString} (Build {buildNumber}) ist zur Review eingereicht!");
    }
}

/// <summary>
/// Duenner HTTP-Wrapper um die App-Store-Connect-API (JWT aus StoreVersionHelper).
/// </summary>
internal sealed class AscClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _apiKeyId;
    private readonly string _issuerId;
    private readonly string _keyPath;
    private string? _jwt;
    private DateTime _jwtExpiresAt;

    public AscClient(string apiKeyId, string issuerId, string keyPath)
    {
        _apiKeyId = apiKeyId;
        _issuerId = issuerId;
        _keyPath = keyPath;
        _http = new HttpClient { BaseAddress = new Uri("https://api.appstoreconnect.apple.com/") };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// ASC-JWTs sind max. 20 min gueltig - bei laengeren Warteschleifen (SUBMIT_MIN_BUILD)
    /// wird der Token daher kurz vor Ablauf erneuert.
    /// </summary>
    private string CurrentJwt()
    {
        if (_jwt is null || DateTime.UtcNow >= _jwtExpiresAt)
        {
            _jwt = StoreVersionHelper.CreateAppStoreConnectJwt(_apiKeyId, _issuerId, _keyPath);
            _jwtExpiresAt = DateTime.UtcNow.AddMinutes(15);
        }
        return _jwt;
    }

    public JsonDocument Get(string url) => Send(HttpMethod.Get, url, null);

    public JsonDocument Post(string url, object body) => Send(HttpMethod.Post, url, body);

    public JsonDocument Patch(string url, object body) => Send(HttpMethod.Patch, url, body);

    private JsonDocument Send(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentJwt());
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");
        }

        var response = _http.Send(request);
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"ASC {method} {url} -> HTTP {(int)response.StatusCode}: {content}");
        }

        return string.IsNullOrEmpty(content)
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(content);
    }

    public void Dispose() => _http.Dispose();
}
