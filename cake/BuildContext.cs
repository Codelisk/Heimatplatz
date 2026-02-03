using Cake.Core;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build;

public class BuildContext : FrostingContext
{
    public new IConfiguration Configuration { get; }

    public string CsprojPath { get; }
    public string ApplicationId { get; }

    // Android settings
    public string AndroidPackageName { get; }
    public string AndroidKeystorePath { get; }
    public string AndroidKeystorePassword { get; }
    public string AndroidKeyAlias { get; }
    public string AndroidKeyPassword { get; }
    public string PlayStoreJsonKeyPath { get; }

    // iOS settings
    public string IosTeamId { get; }
    public string IosBundleId { get; }
    public string MatchGitUrl { get; }
    public string MatchPassword { get; }
    public string AppStoreConnectApiKeyId { get; }
    public string AppStoreConnectIssuerId { get; }
    public string AppStoreConnectKeyPath { get; }

    // Azure Static Web Apps settings
    public string AzureStaticWebAppsApiToken { get; }
    public string ApiBaseUrl { get; }

    // Computed paths
    public string BuildDirectory { get; }
    public string ProjectDirectory { get; }
    public string FastlaneDirectory { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        BuildDirectory = context.Environment.WorkingDirectory.FullPath;
        ProjectDirectory = Path.GetFullPath(Path.Combine(BuildDirectory, ".."));
        FastlaneDirectory = Path.Combine(BuildDirectory, "fastlane");

        Configuration = new ConfigurationBuilder()
            .SetBasePath(BuildDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Project settings
        CsprojPath = Path.GetFullPath(Path.Combine(BuildDirectory, GetConfigValue("Project:CsprojPath", "../src/uno/src/Heimatplatz.App/Heimatplatz.App.csproj")));
        ApplicationId = GetConfigValue("Project:ApplicationId", "com.heimatplatz.app");

        // Android settings (prefer env vars, fallback to config)
        AndroidPackageName = GetConfigValue("Android:PackageName", "ANDROID_PACKAGE_NAME");
        var keystorePath = GetConfigValue("Android:KeystorePath", "ANDROID_KEYSTORE_PATH");
        AndroidKeystorePath = string.IsNullOrEmpty(keystorePath) ? string.Empty : Path.GetFullPath(Path.Combine(BuildDirectory, keystorePath));
        AndroidKeystorePassword = GetConfigValue("Android:KeystorePassword", "ANDROID_KEYSTORE_PASSWORD");
        AndroidKeyAlias = GetConfigValue("Android:KeyAlias", "ANDROID_KEY_ALIAS");
        AndroidKeyPassword = GetConfigValue("Android:KeyPassword", "ANDROID_KEY_PASSWORD");
        var playStoreJsonPath = GetConfigValue("Android:PlayStoreJsonKeyPath", "PLAY_STORE_JSON_KEY_PATH");
        PlayStoreJsonKeyPath = string.IsNullOrEmpty(playStoreJsonPath) ? string.Empty : Path.GetFullPath(Path.Combine(BuildDirectory, playStoreJsonPath));

        // iOS settings
        IosTeamId = GetConfigValue("iOS:TeamId", "APPLE_TEAM_ID");
        IosBundleId = GetConfigValue("iOS:BundleId", "com.heimatplatz.app");
        MatchGitUrl = GetConfigValue("iOS:MatchGitUrl", "MATCH_GIT_URL");
        MatchPassword = GetConfigValue("iOS:MatchPassword", "MATCH_PASSWORD");
        AppStoreConnectApiKeyId = GetConfigValue("iOS:AppStoreConnectApiKeyId", "ASC_KEY_ID");
        AppStoreConnectIssuerId = GetConfigValue("iOS:AppStoreConnectIssuerId", "ASC_ISSUER_ID");
        var ascKeyPath = GetConfigValue("iOS:AppStoreConnectKeyPath", "ASC_KEY_PATH");
        AppStoreConnectKeyPath = string.IsNullOrEmpty(ascKeyPath) ? string.Empty : Path.GetFullPath(Path.Combine(BuildDirectory, ascKeyPath));

        // Azure Static Web Apps settings
        AzureStaticWebAppsApiToken = GetConfigValue("Azure:StaticWebAppsApiToken", "AZURE_STATIC_WEB_APPS_API_TOKEN");
        ApiBaseUrl = GetConfigValue("Azure:ApiBaseUrl", "API_BASE_URL");
    }

    private string GetConfigValue(string configKey, string envVarFallback)
    {
        // First try environment variable
        var envValue = Environment.GetEnvironmentVariable(envVarFallback);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        // Then try config file
        var configValue = Configuration[configKey];
        if (!string.IsNullOrEmpty(configValue))
            return configValue;

        return string.Empty;
    }
}
