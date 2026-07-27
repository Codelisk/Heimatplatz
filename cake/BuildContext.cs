using Cake.Core;
using Cake.Frosting;
using Microsoft.Extensions.Configuration;

namespace Build;

public class BuildContext : FrostingContext
{
    public new IConfiguration Configuration { get; }

    public string CsprojPath { get; }
    public string ApplicationId { get; }

    /// <summary>
    /// Auslieferungskanal des App-Builds (Development|Internal|Production, siehe
    /// src/maui/src/Heimatplatz.Maui/Core/Build/AppChannels.cs). Steuert, ob die App
    /// die Entwicklerwerkzeuge (Flyout "Debug", API-Umschalter) freischaltet.
    /// </summary>
    public string AppChannel { get; }

    // Android settings
    public string AndroidReleaseTrack { get; }
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

    // Web-Deploy settings (Hetzner, Astro-Web-SSR via rsync)
    public string ApiBaseUrl { get; }
    public string ApiBaseUrlTest { get; }
    // Rybbit-Tracking-Snippet (siehe AstroWeb.Build): Prod- und Test-Bundle bekommen je eine
    // EIGENE Rybbit-Site-Id, damit QA-Traffic in einer getrennten Statistik landet und die
    // Prod-Zahlen nicht verfaelscht (Test-Site: test.heimatplatz.at).
    public string RybbitSiteId { get; }
    public string RybbitSiteIdTest { get; }
    public string HetznerHost { get; }
    public string HetznerUser { get; }
    public string HetznerSshKeyPath { get; }
    public string HetznerWebRoot { get; }
    public string HetznerWebRootTest { get; }

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
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Project settings
        CsprojPath = Path.GetFullPath(Path.Combine(BuildDirectory, GetConfigValue("Project:CsprojPath", "../src/maui/src/Heimatplatz.Maui/Heimatplatz.Maui.csproj")));
        ApplicationId = GetConfigValue("Project:ApplicationId", "com.heimatplatz.app");

        // Android settings (prefer env vars, fallback to config)
        // Track kommt aus appsettings.json oder der Env-Var Android__Release__Track
        // (release-android.ps1 -Track ...), die AddEnvironmentVariables mit einliest.
        AndroidReleaseTrack = Configuration["Android:Release:Track"] ?? "internal";

        // Play liefert dieselbe AAB an jeden Track aus - ein Binary kennt seinen Track
        // zur Laufzeit also nicht. Der Kanal wird deshalb hier festgelegt: alles ausser
        // dem Production-Track ist eine interne Auslieferung und bekommt die
        // Entwicklerwerkzeuge. HEIMATPLATZ_CHANNEL uebersteuert das bei Bedarf
        // (z.B. bewusster Production-Build fuer eine Track-Promotion).
        AppChannel = Environment.GetEnvironmentVariable("HEIMATPLATZ_CHANNEL") is { Length: > 0 } explicitChannel
            ? explicitChannel
            : AndroidReleaseTrack.Equals("production", StringComparison.OrdinalIgnoreCase)
                ? "Production"
                : "Internal";

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

        // Web-Deploy settings (Hetzner)
        ApiBaseUrl = GetConfigValue("Web:ApiBaseUrl", "API_BASE_URL");
        ApiBaseUrlTest = GetConfigValue("Web:ApiBaseUrlTest", "API_BASE_URL_TEST");
        RybbitSiteId = GetConfigValue("Web:RybbitSiteId", "RYBBIT_SITE_ID");
        RybbitSiteIdTest = GetConfigValue("Web:RybbitSiteIdTest", "RYBBIT_SITE_ID_TEST");
        HetznerHost = GetConfigValue("Hetzner:Host", "HETZNER_HOST");
        HetznerUser = GetConfigValue("Hetzner:User", "HETZNER_USER");
        var hetznerKeyPath = GetConfigValue("Hetzner:SshKeyPath", "HETZNER_SSH_KEY_PATH");
        HetznerSshKeyPath = string.IsNullOrEmpty(hetznerKeyPath) ? string.Empty : Path.GetFullPath(Path.Combine(BuildDirectory, hetznerKeyPath));
        HetznerWebRoot = GetConfigValue("Hetzner:WebRoot", "HETZNER_WEB_ROOT");
        HetznerWebRootTest = GetConfigValue("Hetzner:WebRootTest", "HETZNER_WEB_ROOT_TEST");
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
