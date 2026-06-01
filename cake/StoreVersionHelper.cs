using System.Diagnostics;
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

            // Parse output like: "Result: [144]"
            var match = Regex.Match(output, @"Result:\s*\[(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var versionCode))
            {
                return versionCode;
            }

            // Try alternative format
            match = Regex.Match(output, @"Found '(\d+)' version codes");
            if (match.Success && int.TryParse(match.Groups[1].Value, out versionCode))
            {
                return versionCode;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the latest build number from TestFlight.
    /// `latest_testflight_build_number` does NOT honor the APP_STORE_CONNECT_API_KEY_* env vars
    /// for username-less auth, so we materialize a temporary api_key JSON and pass it via
    /// `api_key_path:` (the form Spaceship accepts).
    /// </summary>
    public static int? GetTestFlightBuildNumber(string apiKeyId, string issuerId, string keyPath, string bundleId, string fastlaneDir)
    {
        string? tempJsonPath = null;
        try
        {
            // Fastlane's Spaceship requires the PEM contents inline as "key" (not "key_filepath")
            var pemContents = File.ReadAllText(keyPath);
            var keyEscaped = pemContents
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");

            tempJsonPath = Path.Combine(Path.GetTempPath(), $"asc_api_key_{Guid.NewGuid():N}.json");
            var apiKeyJson = $@"{{
  ""key_id"": ""{apiKeyId}"",
  ""issuer_id"": ""{issuerId}"",
  ""key"": ""{keyEscaped}"",
  ""duration"": 1200,
  ""in_house"": false
}}";
            File.WriteAllText(tempJsonPath, apiKeyJson);

            var processInfo = new ProcessStartInfo
            {
                FileName = "fastlane",
                Arguments = $"run latest_testflight_build_number app_identifier:{bundleId} api_key_path:\"{tempJsonPath}\"",
                WorkingDirectory = fastlaneDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) return null;

            // Parse output like: "Result: 144"
            var match = Regex.Match(output, @"Result:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var buildNumber))
            {
                return buildNumber;
            }

            // Try alternative format "Latest build number: 144"
            match = Regex.Match(output, @"build number[:\s]+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out buildNumber))
            {
                return buildNumber;
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (tempJsonPath != null && File.Exists(tempJsonPath))
            {
                try { File.Delete(tempJsonPath); } catch { /* best effort */ }
            }
        }
    }

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
