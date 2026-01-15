using Heimatplatz.UITests.Configuration;
using NUnit.Framework;
using Uno.UITest;

namespace Heimatplatz.UITests.Infrastructure;

/// <summary>
/// Hilfsmethoden fuer plattform-spezifische Operationen.
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// Prueft ob der aktuelle Test auf WebAssembly laeuft.
    /// </summary>
    public static bool IsWebAssembly => Constants.CurrentPlatform == Platform.Browser;

    /// <summary>
    /// Prueft ob der aktuelle Test auf Android laeuft.
    /// </summary>
    public static bool IsAndroid => Constants.CurrentPlatform == Platform.Android;

    /// <summary>
    /// Prueft ob der aktuelle Test auf iOS laeuft.
    /// </summary>
    public static bool IsiOS => Constants.CurrentPlatform == Platform.iOS;

    /// <summary>
    /// Prueft ob der aktuelle Test auf einem mobilen Geraet laeuft.
    /// </summary>
    public static bool IsMobile => IsAndroid || IsiOS;

    /// <summary>
    /// Fuehrt eine Aktion nur auf der angegebenen Plattform aus.
    /// </summary>
    public static void OnPlatform(Platform platform, Action action)
    {
        if (Constants.CurrentPlatform == platform)
        {
            action();
        }
    }

    /// <summary>
    /// Fuehrt eine Aktion auf WebAssembly aus.
    /// </summary>
    public static void OnWebAssembly(Action action) => OnPlatform(Platform.Browser, action);

    /// <summary>
    /// Fuehrt eine Aktion auf Android aus.
    /// </summary>
    public static void OnAndroid(Action action) => OnPlatform(Platform.Android, action);

    /// <summary>
    /// Fuehrt eine Aktion auf iOS aus.
    /// </summary>
    public static void OniOS(Action action) => OnPlatform(Platform.iOS, action);

    /// <summary>
    /// Gibt einen plattform-spezifischen Wert zurueck.
    /// </summary>
    public static T ForPlatform<T>(T webAssembly, T android, T iOS)
    {
        return Constants.CurrentPlatform switch
        {
            Platform.Browser => webAssembly,
            Platform.Android => android,
            Platform.iOS => iOS,
            _ => webAssembly
        };
    }

    /// <summary>
    /// Ignoriert den Test auf der angegebenen Plattform.
    /// </summary>
    public static void SkipOnPlatform(Platform platform, string reason = "")
    {
        if (Constants.CurrentPlatform == platform)
        {
            var message = string.IsNullOrEmpty(reason)
                ? $"Test wird auf {platform} uebersprungen."
                : $"Test wird auf {platform} uebersprungen: {reason}";

            Assert.Ignore(message);
        }
    }

    /// <summary>
    /// Ignoriert den Test auf WebAssembly.
    /// </summary>
    public static void SkipOnWebAssembly(string reason = "") =>
        SkipOnPlatform(Platform.Browser, reason);

    /// <summary>
    /// Ignoriert den Test auf Android.
    /// </summary>
    public static void SkipOnAndroid(string reason = "") =>
        SkipOnPlatform(Platform.Android, reason);

    /// <summary>
    /// Ignoriert den Test auf iOS.
    /// </summary>
    public static void SkipOniOS(string reason = "") =>
        SkipOnPlatform(Platform.iOS, reason);
}
