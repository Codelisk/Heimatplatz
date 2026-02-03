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
    /// Gets the latest build number from TestFlight
    /// </summary>
    public static int? GetTestFlightBuildNumber(string apiKeyId, string issuerId, string keyPath, string bundleId, string fastlaneDir)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "fastlane",
                Arguments = $"run latest_testflight_build_number app_identifier:{bundleId}",
                WorkingDirectory = fastlaneDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // App Store Connect API authentication
            processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_ID"] = apiKeyId;
            processInfo.Environment["APP_STORE_CONNECT_API_KEY_ISSUER_ID"] = issuerId;
            processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_FILEPATH"] = keyPath;

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
