using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployAndroid")]
[IsDependentOn(typeof(ComplianceCheckTask))]
[IsDependentOn(typeof(BuildAndroidTask))]
public sealed class DeployAndroidTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy Android Task (Play Store Internal) ===");

        var artifactsDir = Path.Combine(context.ProjectDirectory, "artifacts", "android");

        var aabFiles = Directory.GetFiles(artifactsDir, "*.aab", SearchOption.AllDirectories);
        var apkFiles = Directory.GetFiles(artifactsDir, "*.apk", SearchOption.AllDirectories);

        string packagePath;
        bool isApk;

        // Prefer AAB over APK (Play Store requires AAB for most apps)
        if (aabFiles.Length > 0)
        {
            packagePath = aabFiles.FirstOrDefault(f => f.Contains("-Signed")) ?? aabFiles.First();
            isApk = false;
            context.Information($"Found AAB: {packagePath}");
        }
        else if (apkFiles.Length > 0)
        {
            packagePath = apkFiles.FirstOrDefault(f => f.Contains("-Signed")) ?? apkFiles.First();
            isApk = true;
            context.Information($"Found APK: {packagePath}");
        }
        else
        {
            throw new FileNotFoundException($"No AAB or APK file found in {artifactsDir}");
        }

        if (string.IsNullOrEmpty(context.PlayStoreJsonKeyPath))
        {
            context.Warning("PLAY_STORE_JSON_KEY_PATH not configured. Skipping upload.");
            context.Information("To enable upload, set the PLAY_STORE_JSON_KEY_PATH environment variable or configure it in appsettings.json");
            return;
        }

        if (!File.Exists(context.PlayStoreJsonKeyPath))
        {
            throw new FileNotFoundException($"Play Store JSON key not found: {context.PlayStoreJsonKeyPath}");
        }

        var fastlaneDir = context.FastlaneDirectory;
        context.Information($"Running Fastlane from: {fastlaneDir}");

        var processInfo = new ProcessStartInfo
        {
            FileName = "fastlane",
            Arguments = "android internal",
            WorkingDirectory = fastlaneDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (isApk)
        {
            processInfo.Environment["SUPPLY_APK"] = packagePath;
        }
        else
        {
            processInfo.Environment["SUPPLY_AAB"] = packagePath;
        }
        processInfo.Environment["SUPPLY_JSON_KEY"] = context.PlayStoreJsonKeyPath;
        processInfo.Environment["SUPPLY_PACKAGE_NAME"] = context.AndroidPackageName;

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Fastlane process");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        context.Information(output);
        if (!string.IsNullOrEmpty(error))
        {
            context.Warning(error);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Fastlane failed with exit code {process.ExitCode}");
        }

        context.Information("Android deployment to Play Store internal track completed!");
    }
}
