using Cake.Frosting;

namespace Build.Tasks;

[TaskName("DeployAstro")]
[IsDependentOn(typeof(BuildAstroTask))]
public sealed class DeployAstroTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
        => AstroWeb.Deploy(context, context.HetznerWebRoot, "web");
}
