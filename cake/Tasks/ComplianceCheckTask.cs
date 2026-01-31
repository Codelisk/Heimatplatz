using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Pre-flight check to verify there are no pending agreements in Play Console or App Store Connect.
/// This prevents cryptic Fastlane failures by detecting agreement issues before deployment starts.
/// </summary>
[TaskName("ComplianceCheck")]
public sealed class ComplianceCheckTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Store Compliance Check ===");
        context.Information("Checking for pending agreements in Play Console and App Store Connect...");
        context.Information("");

        var hasErrors = false;

        // Check Google Play Console
        if (!string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) && File.Exists(context.PlayStoreJsonKeyPath))
        {
            context.Information("Checking Google Play Console...");
            var googleResult = StoreComplianceHelper.CheckGooglePlayCompliance(
                context.PlayStoreJsonKeyPath,
                context.AndroidPackageName,
                context.FastlaneDirectory);

            if (!googleResult.IsCompliant)
            {
                hasErrors = true;
                context.Error("╔══════════════════════════════════════════════════════════════╗");
                context.Error("║  GOOGLE PLAY CONSOLE - ACTION REQUIRED                       ║");
                context.Error("╠══════════════════════════════════════════════════════════════╣");
                context.Error($"║  {googleResult.ErrorMessage}");
                context.Error("║                                                              ║");
                context.Error($"║  Please visit: {googleResult.ConsoleUrl}");
                context.Error("║  Accept all pending agreements, then re-run the build.       ║");
                context.Error("╚══════════════════════════════════════════════════════════════╝");
                context.Information("");
            }
            else
            {
                context.Information("  Google Play Console: No pending agreements detected");
                if (!string.IsNullOrEmpty(googleResult.ErrorMessage))
                {
                    context.Warning($"    {googleResult.ErrorMessage}");
                }
            }
        }
        else
        {
            context.Information("  - Google Play Console: Skipped (credentials not configured)");
        }

        // Check App Store Connect
        if (!string.IsNullOrEmpty(context.AppStoreConnectKeyPath) && File.Exists(context.AppStoreConnectKeyPath))
        {
            context.Information("Checking App Store Connect...");
            var appleResult = StoreComplianceHelper.CheckAppStoreConnectCompliance(
                context.AppStoreConnectApiKeyId,
                context.AppStoreConnectIssuerId,
                context.AppStoreConnectKeyPath,
                context.IosBundleId,
                context.FastlaneDirectory);

            if (!appleResult.IsCompliant)
            {
                hasErrors = true;
                context.Error("╔══════════════════════════════════════════════════════════════╗");
                context.Error("║  APP STORE CONNECT - ACTION REQUIRED                         ║");
                context.Error("╠══════════════════════════════════════════════════════════════╣");
                context.Error($"║  {appleResult.ErrorMessage}");
                context.Error("║                                                              ║");
                context.Error($"║  Please visit: {appleResult.ConsoleUrl}");
                context.Error("║  Accept all pending agreements, then re-run the build.       ║");
                context.Error("╚══════════════════════════════════════════════════════════════╝");
                context.Information("");
            }
            else
            {
                context.Information("  App Store Connect: No pending agreements detected");
                if (!string.IsNullOrEmpty(appleResult.ErrorMessage))
                {
                    context.Warning($"    {appleResult.ErrorMessage}");
                }
            }
        }
        else
        {
            context.Information("  - App Store Connect: Skipped (credentials not configured)");
        }

        context.Information("");

        if (hasErrors)
        {
            throw new InvalidOperationException(
                "Store compliance check failed. Please accept all pending agreements before deploying. " +
                "See the error messages above for details and links to the respective consoles.");
        }

        context.Information("=== Compliance Check Passed ===");
        context.Information("");
    }
}
