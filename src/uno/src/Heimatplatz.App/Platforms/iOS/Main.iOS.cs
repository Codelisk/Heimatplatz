using Uno.UI.Hosting;

namespace Heimatplatz.App.iOS;

public class EntryPoint
{
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit(builder => builder.UseUIApplicationDelegate<ShinyAppDelegate>())
            .Build();

        host.Run();
    }
}
