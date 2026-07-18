using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Baut das Astro-Web-Bundle gegen die Test-API (PUBLIC_API_BASE_URL=Web:ApiBaseUrlTest)
/// und deployt es in das Test-Web-Verzeichnis auf dem Hetzner-Server; startet den
/// web-test-Container neu. Bewusst NICHT Teil von DeployAll - das Test-Web wird
/// unabhaengig (und in der Regel oefter) deployt als Prod: erst Test, dann Promote.
/// </summary>
[TaskName("DeployAstroTest")]
public sealed class DeployAstroTestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        AstroWeb.Build(context, context.ApiBaseUrlTest);
        AstroWeb.Deploy(context, context.HetznerWebRootTest, "web-test");
        DeployHealth.WaitFor(context, context.Configuration["Hetzner:WebTestHealthUrl"]);
    }
}
