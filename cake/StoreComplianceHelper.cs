using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Build;

/// <summary>
/// Helper class to check for pending agreements in Google Play Console and App Store Connect
/// </summary>
public static class StoreComplianceHelper
{
    public record ComplianceResult(bool IsCompliant, string? ErrorMessage, string? ConsoleUrl);

    /// <summary>
    /// Checks if there are pending agreements in Google Play Console
    /// </summary>
    public static ComplianceResult CheckGooglePlayCompliance(string jsonKeyPath, string packageName, string fastlaneDir)
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
            if (process == null)
            {
                return new ComplianceResult(false, "Failed to start Fastlane process", null);
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var combinedOutput = output + error;

            // Check for common agreement-related errors
            if (ContainsAgreementError(combinedOutput, out var googleError))
            {
                return new ComplianceResult(
                    false,
                    googleError,
                    "https://play.google.com/console/developers");
            }

            // Check for permission errors that might indicate agreement issues
            if (combinedOutput.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                combinedOutput.Contains("403", StringComparison.OrdinalIgnoreCase))
            {
                if (combinedOutput.Contains("agreement", StringComparison.OrdinalIgnoreCase) ||
                    combinedOutput.Contains("accept", StringComparison.OrdinalIgnoreCase) ||
                    combinedOutput.Contains("terms", StringComparison.OrdinalIgnoreCase))
                {
                    return new ComplianceResult(
                        false,
                        "Access denied - there may be pending agreements in Play Console",
                        "https://play.google.com/console/developers");
                }
            }

            if (process.ExitCode == 0)
            {
                return new ComplianceResult(true, null, null);
            }

            return new ComplianceResult(true, null, null);
        }
        catch (Exception ex)
        {
            return new ComplianceResult(true, $"Warning: Could not verify compliance: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Checks if there are pending agreements in App Store Connect
    /// </summary>
    public static ComplianceResult CheckAppStoreConnectCompliance(
        string apiKeyId,
        string issuerId,
        string keyPath,
        string bundleId,
        string fastlaneDir)
    {
        try
        {
            var precheckResult = RunAppStoreConnectCheck(
                "run precheck",
                apiKeyId, issuerId, keyPath, bundleId, fastlaneDir);

            if (!precheckResult.IsCompliant)
            {
                return precheckResult;
            }

            return RunAppStoreConnectCheck(
                $"run latest_testflight_build_number app_identifier:{bundleId}",
                apiKeyId, issuerId, keyPath, bundleId, fastlaneDir);
        }
        catch (Exception ex)
        {
            return new ComplianceResult(true, $"Warning: Could not verify compliance: {ex.Message}", null);
        }
    }

    private static ComplianceResult RunAppStoreConnectCheck(
        string arguments,
        string apiKeyId,
        string issuerId,
        string keyPath,
        string bundleId,
        string fastlaneDir)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "fastlane",
                Arguments = arguments,
                WorkingDirectory = fastlaneDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_ID"] = apiKeyId;
            processInfo.Environment["APP_STORE_CONNECT_API_KEY_ISSUER_ID"] = issuerId;
            processInfo.Environment["APP_STORE_CONNECT_API_KEY_KEY_FILEPATH"] = keyPath;
            processInfo.Environment["PRECHECK_APP_IDENTIFIER"] = bundleId;

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return new ComplianceResult(false, "Failed to start Fastlane process", null);
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var combinedOutput = output + error;

            if (ContainsAppStoreAgreementError(combinedOutput, out var ascError))
            {
                return new ComplianceResult(
                    false,
                    ascError,
                    "https://appstoreconnect.apple.com/agreements");
            }

            if (combinedOutput.Contains("FORBIDDEN_ERROR", StringComparison.OrdinalIgnoreCase) ||
                combinedOutput.Contains("contractsAcceptanceRequired", StringComparison.OrdinalIgnoreCase))
            {
                return new ComplianceResult(
                    false,
                    "App Store Connect requires you to accept new agreements",
                    "https://appstoreconnect.apple.com/agreements");
            }

            if (process.ExitCode == 0)
            {
                return new ComplianceResult(true, null, null);
            }

            if (ContainsAppStoreAgreementError(combinedOutput, out var otherError))
            {
                return new ComplianceResult(
                    false,
                    otherError,
                    "https://appstoreconnect.apple.com/agreements");
            }

            return new ComplianceResult(true, null, null);
        }
        catch (Exception ex)
        {
            return new ComplianceResult(true, $"Warning: Could not verify compliance: {ex.Message}", null);
        }
    }

    private static bool ContainsAgreementError(string output, out string errorMessage)
    {
        errorMessage = string.Empty;

        var patterns = new[]
        {
            (@"(?i)developer\s+agreement", "You need to accept the Developer Agreement in Google Play Console"),
            (@"(?i)distribution\s+agreement", "You need to accept the Distribution Agreement in Google Play Console"),
            (@"(?i)terms\s+of\s+service.*accept", "You need to accept updated Terms of Service in Google Play Console"),
            (@"(?i)policy.*accept", "You need to accept updated policies in Google Play Console"),
            (@"(?i)agreement.*pending", "There are pending agreements in Google Play Console"),
            (@"(?i)accept.*agreement", "You need to accept agreements in Google Play Console"),
            (@"(?i)sign.*agreement", "You need to sign agreements in Google Play Console"),
        };

        foreach (var (pattern, message) in patterns)
        {
            if (Regex.IsMatch(output, pattern))
            {
                errorMessage = message;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAppStoreAgreementError(string output, out string errorMessage)
    {
        errorMessage = string.Empty;

        var patterns = new[]
        {
            (@"(?i)paid\s+applications?\s+agreement", "You need to accept the Paid Applications Agreement in App Store Connect"),
            (@"(?i)apple\s+developer\s+program\s+license\s+agreement", "You need to accept the Apple Developer Program License Agreement"),
            (@"(?i)contracts?\s+and\s+agreements?", "You need to accept contracts and agreements in App Store Connect"),
            (@"(?i)required\s+agreement\s+is\s+missing", "A required agreement is missing - please accept it in App Store Connect"),
            (@"(?i)agreement.*expired", "Your agreement has expired - please renew in App Store Connect"),
            (@"(?i)agreement.*missing\s+or.*expired", "A required agreement is missing or has expired in App Store Connect"),
            (@"(?i)in-effect\s+agreement", "A required agreement needs to be signed in App Store Connect"),
            (@"(?i)contract.*accept", "You need to accept contracts in App Store Connect"),
            (@"(?i)FORBIDDEN.*agreement", "Access denied - you need to accept agreements in App Store Connect"),
            (@"(?i)banking.*information", "You need to update banking information in App Store Connect"),
            (@"(?i)tax\s+forms?", "You need to complete tax forms in App Store Connect"),
            (@"contractsAcceptanceRequired", "You need to accept new contracts in App Store Connect"),
        };

        foreach (var (pattern, message) in patterns)
        {
            if (Regex.IsMatch(output, pattern))
            {
                errorMessage = message;
                return true;
            }
        }

        return false;
    }
}
