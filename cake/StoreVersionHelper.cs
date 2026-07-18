using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Build;

/// <summary>
/// Helper class to query version codes from Google Play and TestFlight
/// </summary>
public static class StoreVersionHelper
{
    /// <summary>
    /// Gets the highest version code across all Google Play tracks.
    /// Uses the Play Developer API directly (PlayStoreClient) - no fastlane/Ruby needed,
    /// so this also works on Windows. The edit is never committed and expires server-side.
    /// The fastlaneDir parameter is kept for call-site compatibility and is unused.
    /// </summary>
    public static int? GetGooglePlayVersionCode(string jsonKeyPath, string packageName, string fastlaneDir)
    {
        try
        {
            using var client = new PlayStoreClient(jsonKeyPath, packageName);
            var editId = client.CreateEdit();
            var highest = client.GetHighestVersionCode(editId);
            return highest > 0 ? highest : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StoreVersionHelper] Google Play query failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the highest build number ever uploaded to TestFlight across ALL versions.
    /// Queries the App Store Connect API directly because fastlane's
    /// `latest_testflight_build_number` only looks at the most recently uploaded version
    /// (so a 1.64.0 upload would hide an existing 1.66.0/66 build).
    /// </summary>
    public static int? GetTestFlightBuildNumber(string apiKeyId, string issuerId, string keyPath, string bundleId, string fastlaneDir)
    {
        try
        {
            var jwt = CreateAppStoreConnectJwt(apiKeyId, issuerId, keyPath);
            using var http = new HttpClient { BaseAddress = new Uri("https://api.appstoreconnect.apple.com/") };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Resolve app numeric id by bundle id
            var appsResp = GetWithRetry(http, $"v1/apps?filter[bundleId]={Uri.EscapeDataString(bundleId)}&fields[apps]=bundleId&limit=1");
            if (!appsResp.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[StoreVersionHelper] apps lookup failed: {(int)appsResp.StatusCode} {appsResp.ReasonPhrase} - {appsResp.Content.ReadAsStringAsync().Result}");
                return null;
            }
            var appsJson = appsResp.Content.ReadAsStringAsync().Result;
            using var appsDoc = JsonDocument.Parse(appsJson);
            var dataArr = appsDoc.RootElement.GetProperty("data");
            if (dataArr.GetArrayLength() == 0)
            {
                Console.Error.WriteLine($"[StoreVersionHelper] no app found for bundleId={bundleId}");
                return null;
            }
            var appId = dataArr[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(appId)) return null;

            // List builds for the app, paginate through all pages.
            // 'version' on the Build resource is the build number (as a string).
            var max = 0;
            var seenAny = false;
            var nextUrl = $"v1/builds?filter[app]={appId}&fields[builds]=version&limit=200";
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var resp = GetWithRetry(http, nextUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine($"[StoreVersionHelper] builds lookup failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {resp.Content.ReadAsStringAsync().Result}");
                    return seenAny ? max : null;
                }
                var json = resp.Content.ReadAsStringAsync().Result;
                using var doc = JsonDocument.Parse(json);
                var builds = doc.RootElement.GetProperty("data");
                foreach (var build in builds.EnumerateArray())
                {
                    if (!build.TryGetProperty("attributes", out var attrs)) continue;
                    if (!attrs.TryGetProperty("version", out var versionEl)) continue;
                    var versionStr = versionEl.GetString();
                    if (int.TryParse(versionStr, out var buildNumber))
                    {
                        if (!seenAny || buildNumber > max) max = buildNumber;
                        seenAny = true;
                    }
                }

                nextUrl = null;
                if (doc.RootElement.TryGetProperty("links", out var links)
                    && links.TryGetProperty("next", out var nextEl)
                    && nextEl.ValueKind == JsonValueKind.String)
                {
                    var nextAbs = nextEl.GetString();
                    if (!string.IsNullOrEmpty(nextAbs))
                    {
                        // Strip absolute base so HttpClient.BaseAddress applies.
                        nextUrl = nextAbs.StartsWith("https://api.appstoreconnect.apple.com/")
                            ? nextAbs.Substring("https://api.appstoreconnect.apple.com/".Length)
                            : nextAbs;
                    }
                }
            }

            if (!seenAny)
            {
                Console.Error.WriteLine($"[StoreVersionHelper] appId={appId} resolved but zero builds returned by App Store Connect");
            }
            return seenAny ? max : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StoreVersionHelper] TestFlight query failed: {ex}");
            return null;
        }
    }

    /// <summary>
    /// App Store Connect intermittently returns transient 401/403/5xx right after an
    /// account-level change (e.g. accepting an agreement) propagates. Retry a few times
    /// before giving up.
    /// </summary>
    private static HttpResponseMessage GetWithRetry(HttpClient http, string url, int attempts = 5, int delayMs = 3000)
    {
        HttpResponseMessage? last = null;
        for (var i = 0; i < attempts; i++)
        {
            if (i > 0) Thread.Sleep(delayMs);
            last = http.GetAsync(url).Result;
            if (last.IsSuccessStatusCode) return last;
            Console.Error.WriteLine($"[StoreVersionHelper] attempt {i + 1}/{attempts} for {url} -> {(int)last.StatusCode} {last.ReasonPhrase}");
        }
        return last!;
    }

    internal static string CreateAppStoreConnectJwt(string apiKeyId, string issuerId, string keyPath)
    {
        var header = $"{{\"alg\":\"ES256\",\"kid\":\"{apiKeyId}\",\"typ\":\"JWT\"}}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = now + 1200; // 20 min (ASC max)
        var payload = $"{{\"iss\":\"{issuerId}\",\"iat\":{now},\"exp\":{exp},\"aud\":\"appstoreconnect-v1\"}}";

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(File.ReadAllText(keyPath));
        var signature = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Gets the highest version code from both stores
    /// </summary>
    public static int GetHighestStoreVersion(BuildContext context)
    {
        int highest = 0;

        // Query Google Play
        if (!string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) && File.Exists(context.PlayStoreJsonKeyPath))
        {
            var googlePlayVersion = GetGooglePlayVersionCode(
                context.PlayStoreJsonKeyPath,
                context.AndroidPackageName,
                context.FastlaneDirectory);

            if (googlePlayVersion.HasValue && googlePlayVersion.Value > highest)
            {
                highest = googlePlayVersion.Value;
            }
        }

        // Query TestFlight
        if (!string.IsNullOrEmpty(context.AppStoreConnectKeyPath) && File.Exists(context.AppStoreConnectKeyPath))
        {
            var testFlightVersion = GetTestFlightBuildNumber(
                context.AppStoreConnectApiKeyId,
                context.AppStoreConnectIssuerId,
                context.AppStoreConnectKeyPath,
                context.IosBundleId,
                context.FastlaneDirectory);

            if (testFlightVersion.HasValue && testFlightVersion.Value > highest)
            {
                highest = testFlightVersion.Value;
            }
        }

        return highest;
    }
}
