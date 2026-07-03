#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Heimatplatz.Maui.Core.DeepLink;

/// <summary>
/// Shiny Android-Lifecycle-Hook fuer Deep Links (heimatplatz://...).
/// Shiny.Hosting.Maui (UseShiny) verdrahtet OnCreate/OnNewIntent der MainActivity
/// automatisch mit diesen Hooks - kein Code in der MainActivity noetig.
/// Registriert in AddDeepLinkFeature.
/// </summary>
public class DeepLinkIntentHandler(
    IDeepLinkService deepLinkService,
    ILogger<DeepLinkIntentHandler> logger)
    : IAndroidLifecycle.IOnActivityNewIntent, IAndroidLifecycle.IOnActivityOnCreate
{
    /// <summary>
    /// Kaltstart: Shell/Navigation stehen bei OnCreate noch nicht - kurz warten.
    /// </summary>
    private static readonly TimeSpan ColdStartDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// App laeuft bereits (SingleTop): Intent kommt via OnNewIntent
    /// </summary>
    public void Handle(Activity activity, Intent intent)
        => TryHandleIntent(intent, TimeSpan.Zero);

    /// <summary>
    /// Kaltstart ueber Deep Link: Intent haengt an der Activity
    /// </summary>
    public void ActivityOnCreate(Activity activity, Bundle? savedInstanceState)
        => TryHandleIntent(activity.Intent, ColdStartDelay);

    private void TryHandleIntent(Intent? intent, TimeSpan delay)
    {
        if (intent?.Action != Intent.ActionView)
            return;

        var dataString = intent.DataString;
        if (dataString is null || !Uri.TryCreate(dataString, UriKind.Absolute, out var uri))
            return;

        if (!deepLinkService.CanHandleUri(uri))
            return;

        _ = HandleSafeAsync(uri, delay);
    }

    private async Task HandleSafeAsync(Uri uri, TimeSpan delay)
    {
        try
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay);

            await deepLinkService.HandleDeepLinkAsync(uri);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DeepLink] Fehler beim Verarbeiten des Intents: {Uri}", uri);
        }
    }
}
#endif
