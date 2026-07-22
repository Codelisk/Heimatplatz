using Cake.Frosting;

namespace Build.Tasks;

[TaskName("BuildAstro")]
public sealed class BuildAstroTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
        => AstroWeb.Build(context, context.ApiBaseUrl, context.RybbitSiteId);
}
