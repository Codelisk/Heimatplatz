using Cake.Common;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .UseLifetime<BuildLifetime>()
            .Run(args);
    }
}

public sealed class BuildLifetime : FrostingLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        context.Log.Information("Pulling latest changes from all remotes...");
        context.StartProcess("git", new Cake.Core.IO.ProcessSettings
        {
            Arguments = "pull --all",
            WorkingDirectory = context.ProjectDirectory
        });
        context.Log.Information("Git pull completed.");
    }

    public override void Teardown(BuildContext context, ITeardownContext info)
    {
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(Tasks.VersionBumpTask))]
public sealed class DefaultTask : FrostingTask
{
}
