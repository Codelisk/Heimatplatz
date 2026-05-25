using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Hosting;

namespace Heimatplatz.Core.Startup.Services;

/// <summary>
/// Stellt den IServiceProvider sofort nach Build des IHost zur Verfuegung.
///
/// Hintergrund: <c>App.Services</c> haengt von <c>Host?.Services</c> ab. <c>App.Host</c>
/// wird aber erst nach <c>await builder.NavigateAsync&lt;Shell&gt;()</c> zugewiesen.
/// Waehrend der initialen Navigation feuert <c>MainPage.Loaded</c> bereits, dort ist
/// <c>App.Services</c> noch null. <see cref="IServiceInitialize"/> wird von Uno.Extensions
/// direkt nach dem Host-Build aufgerufen - lange bevor die Navigation abgeschlossen ist -
/// und ist daher der zuverlaessige Punkt, um den IServiceProvider statisch verfuegbar
/// zu machen.
/// </summary>
public class ApplicationServiceProvider(IServiceProvider services) : IServiceInitialize
{
    public static IServiceProvider? Current { get; private set; }

    public void Initialize()
    {
        Current = services;
    }
}
