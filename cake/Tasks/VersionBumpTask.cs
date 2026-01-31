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

        // Query stores for latest version codes
        context.Information("Querying Google Play and TestFlight for latest version codes...");

        int? googlePlayVersion = null;
        int? testFlightVersion = null;

        // Query Google Play
        if (!string.IsNullOrEmpty(context.PlayStoreJsonKeyPath) && File.Exists(context.PlayStoreJsonKeyPath))
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
            context.Warning("Google Play JSON key not configured, skipping Google Play version check");
        }

        // Query TestFlight
        if (!string.IsNullOrEmpty(context.AppStoreConnectKeyPath) && File.Exists(context.AppStoreConnectKeyPath))
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
            context.Warning("App Store Connect key not configured, skipping TestFlight version check");
        }

        // Determine the highest version from all sources
        var highestStoreVersion = Math.Max(
            googlePlayVersion ?? 0,
            testFlightVersion ?? 0);

        context.Information($"Highest store version: {highestStoreVersion}");

        // New build version = max(current, highest_store) + 1
        var newBuildVersion = Math.Max(currentBuildVersion, highestStoreVersion) + 1;

        // Parse and increment minor version
        var versionParts = currentDisplayVersion.Split('.');
        if (versionParts.Length >= 2)
        {
            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);
            var patch = versionParts.Length >= 3 ? int.Parse(versionParts[2]) : 0;

            // Increment minor, reset patch
            minor++;
            patch = 0;

            var newDisplayVersion = $"{major}.{minor}.{patch}";

            context.Information($"New display version: {newDisplayVersion}");
            context.Information($"New build version: {newBuildVersion}");

            UpdateAllElements(propertyGroups, ns, "ApplicationDisplayVersion", newDisplayVersion);
            UpdateAllElements(propertyGroups, ns, "ApplicationVersion", newBuildVersion.ToString());

            doc.Save(context.CsprojPath);
            context.Information("Version bump completed successfully!");
        }
        else
        {
            throw new InvalidOperationException($"Invalid version format: {currentDisplayVersion}");
        }
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
