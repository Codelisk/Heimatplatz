using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Build;

/// <summary>
/// Helper class to query version codes from Google Play and TestFlight
/// </summary>
public static class StoreVersionHelper
{
    /// <summary>
    /// Gets the latest version code from Google Play internal track
    /// </summary>
    public static int? GetGooglePlayVersionCode(string jsonKeyPath, string packageName, string fastlaneDir)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "fastlane",
                Arguments = "run google_play_track_version_codes track:internal",
                WorkingDirectory = fastlaneDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["SUPPLY_JSON_KEY"] = jsonKeyPath;
            processInfo.Environment["SUPPLY_PACKAGE_NAME"] = packageName;

            using var process = Process.Start(processInfo);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) return null;

            // Parse output like: "Result: [144]" (highest first)
            var match = Regex.Match(output, @"Result:\s*\[([^\]]+)\]");
            if (match.Success)
            {
                var max = 0;
                var seen = false;
                foreach (var token in match.Groups[1].Value.Split(','))
                {
                    if (int.TryParse(token.Trim(), out var v))
                    {
                        if (!seen || v > max) max = v;
                        seen = true;
                    }
                }
                if (seen) return max;
            }

            return null;
        }
        catch
        {
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
            var appsResp = http.GetAsync($"v1/apps?filter[bundleId]={Uri.EscapeDataString(bundleId)}&fields[apps]=bundleId&limit=1").Result;
            if (!appsResp.IsSuccessStatusCode) return null;
            var appsJson = appsResp.Content.ReadAsStringAsync().Result;
            using var appsDoc = JsonDocument.Parse(appsJson);
            var dataArr = appsDoc.RootElement.GetProperty("data");
            if (dataArr.GetArrayLength() == 0) return null;
            var appId = dataArr[0].GetProperty("id").GetString();
            if (string.IsNullOrEmpty(appId)) return null;

            // List builds for the app, paginate through all pages.
            // 'version' on the Build resource is the build number (as a string).
            var max = 0;
            var seenAny = false;
            var nextUrl = $"v1/builds?filter[app]={appId}&fields[builds]=version&limit=200";
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var resp = http.GetAsync(nextUrl).Result;
                if (!resp.IsSuccessStatusCode) return seenAny ? max : null;
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

            return seenAny ? max : null;
        }
        catch
        {
            return null;
        }
    }

    private static string CreateAppStoreConnectJwt(string apiKeyId, string issuerId, string keyPath)
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
