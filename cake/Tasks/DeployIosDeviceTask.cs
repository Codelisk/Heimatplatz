using System.Diagnostics;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployIosDevice")]
[IsDependentOn(typeof(BuildIosDevTask))]
public sealed class DeployIosDeviceTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (!OperatingSystem.IsMacOS())
        {
            context.Warning("iOS device deploy task can only run on macOS. Skipping.");
            return false;
        }
        return true;
    }

    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy iOS Device Task ===");

        // Check if ios-deploy is installed
        if (!IsIosDeployInstalled())
        {
            throw new InvalidOperationException(
                "ios-deploy is not installed. Install it with: brew install ios-deploy");
        }

        var artifactsDir = Path.Combine(context.ProjectDirectory, "artifacts", "ios-dev");

        // Try .app bundle first, then fall back to .ipa (dotnet publish produces .ipa)
        var appBundles = Directory.GetDirectories(artifactsDir, "*.app", SearchOption.AllDirectories);
        string deployPath;
        if (appBundles.Length > 0)
        {
            deployPath = appBundles.First();
        }
        else
        {
            var ipaFiles = Directory.GetFiles(artifactsDir, "*.ipa", SearchOption.AllDirectories);
            if (ipaFiles.Length == 0)
            {
                throw new FileNotFoundException($"No .app bundle or .ipa found in {artifactsDir}");
            }
            deployPath = ipaFiles.First();
        }

        context.Information($"Found artifact: {deployPath}");

        context.Information("Installing and launching on connected device...");
        RunIosDeploy(context, deployPath);

        context.Information("iOS device deployment completed!");
    }

    private bool IsIosDeployInstalled()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ios-deploy",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void RunIosDeploy(BuildContext context, string appPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "ios-deploy",
            Arguments = $"--bundle \"{appPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start ios-deploy process");
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
            throw new InvalidOperationException($"ios-deploy failed with exit code {process.ExitCode}");
        }
    }
}
