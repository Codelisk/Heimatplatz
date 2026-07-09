using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("VersionBump")]
public sealed class VersionBumpTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Version Bump Task ===");
        context.Information($"Csproj path: {context.CsprojPath}");

        if (!File.Exists(context.CsprojPath))
        {
            throw new FileNotFoundException($"Project file not found: {context.CsprojPath}");
        }

        var doc = XDocument.Load(context.CsprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        var propertyGroups = doc.Descendants(ns + "PropertyGroup").ToList();

        var currentDisplayVersion = GetFirstElementValue(propertyGroups, ns, "ApplicationDisplayVersion");
        var currentBuildVersion = int.Parse(GetFirstElementValue(propertyGroups, ns, "ApplicationVersion"));

        context.Information($"Current display version: {currentDisplayVersion}");
        context.Information($"Current build version: {currentBuildVersion}");

        // Query whichever stores are configured for this deploy target.
        int? googlePlayVersion = null;
        int? testFlightVersion = null;

        var googlePlayConfigured = !string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) && File.Exists(context.PlayStoreJsonKeyPath);
        var appStoreConnectConfigured = !string.IsNullOrEmpty(context.AppStoreConnectKeyPath) && File.Exists(context.AppStoreConnectKeyPath);

        // Query Google Play
        if (googlePlayConfigured)
        {
            context.Information("Checking Google Play internal track...");
            googlePlayVersion = StoreVersionHelper.GetGooglePlayVersionCode(
                context.PlayStoreJsonKeyPath,
                context.ApplicationId,
                context.FastlaneDirectory);

            if (googlePlayVersion.HasValue)
            {
                context.Information($"Google Play latest version: {googlePlayVersion.Value}");
            }
            else
            {
                context.Warning("Could not retrieve Google Play version");
            }
        }
        else
        {
            context.Information("Google Play JSON key not configured for this deploy - skipping Google Play version check");
        }

        // Query TestFlight
        if (appStoreConnectConfigured)
        {
            context.Information("Checking TestFlight...");
            testFlightVersion = StoreVersionHelper.GetTestFlightBuildNumber(
                context.AppStoreConnectApiKeyId,
                context.AppStoreConnectIssuerId,
                context.AppStoreConnectKeyPath,
                context.IosBundleId,
                context.FastlaneDirectory);

            if (testFlightVersion.HasValue)
            {
                context.Information($"TestFlight latest version: {testFlightVersion.Value}");
            }
            else
            {
                context.Warning("Could not retrieve TestFlight version");
            }
        }
        else
        {
            context.Information("App Store Connect key not configured for this deploy - skipping TestFlight version check");
        }

        // Stores are the source of truth. If every *configured* store's query fails, fall
        // back to csproj to avoid resetting the build number to 1 (which stores would reject).
        // A store that isn't configured for this deploy target (e.g. Google Play on an
        // iOS-only Codemagic run) doesn't count against this - only actual query failures do.
        var anyConfigured = googlePlayConfigured || appStoreConnectConfigured;
        var anySucceeded = googlePlayVersion.HasValue || testFlightVersion.HasValue;
        if (anyConfigured && !anySucceeded)
        {
            var failedStores = string.Join(" and ", new[]
            {
                googlePlayConfigured ? "Google Play" : null,
                appStoreConnectConfigured ? "TestFlight" : null,
            }.Where(s => s != null));
            throw new InvalidOperationException(
                $"{failedStores} version query failed. Refusing to bump without a known store baseline.");
        }

        var highestStoreVersion = Math.Max(
            googlePlayVersion ?? 0,
            testFlightVersion ?? 0);

        context.Information($"Highest store version: {highestStoreVersion}");

        // New build version = highest_store + 1
        var newBuildVersion = highestStoreVersion + 1;

        // Display version is derived from the new build: {major}.{newBuild}.0
        var versionParts = currentDisplayVersion.Split('.');
        if (versionParts.Length < 1 || !int.TryParse(versionParts[0], out var major))
        {
            throw new InvalidOperationException($"Invalid version format: {currentDisplayVersion}");
        }

        var newDisplayVersion = $"{major}.{newBuildVersion}.0";

        context.Information($"New display version: {newDisplayVersion}");
        context.Information($"New build version: {newBuildVersion}");

        UpdateAllElements(propertyGroups, ns, "ApplicationDisplayVersion", newDisplayVersion);
        UpdateAllElements(propertyGroups, ns, "ApplicationVersion", newBuildVersion.ToString());

        doc.Save(context.CsprojPath);
        context.Information("Version bump completed successfully!");
    }

    private static string GetFirstElementValue(List<XElement> propertyGroups, XNamespace ns, string elementName)
    {
        foreach (var pg in propertyGroups)
        {
            var element = pg.Element(ns + elementName);
            if (element != null)
                return element.Value;
        }
        throw new InvalidOperationException($"Element {elementName} not found in csproj");
    }

    private static void UpdateAllElements(List<XElement> propertyGroups, XNamespace ns, string elementName, string newValue)
    {
        foreach (var pg in propertyGroups)
        {
            var element = pg.Element(ns + elementName);
            if (element != null)
            {
                element.Value = newValue;
            }
        }
    }
}
