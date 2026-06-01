using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployAll")]
[IsDependentOn(typeof(DeployAndroidTask))]
[IsDependentOn(typeof(DeployIosTask))]
[IsDependentOn(typeof(DeployAstroTask))]
public sealed class DeployAllTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("=== Deploy All Task ===");
        context.Information("Android, iOS, and Astro web deployments completed!");
    }
}
