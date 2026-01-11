namespace Heimatplatz.Core.UITests.AutomationIds;

/// <summary>
/// Zentrale AutomationIds fuer Core UI-Elemente.
/// Diese IDs muessen in den entsprechenden XAML-Dateien mit
/// AutomationProperties.AutomationId gesetzt werden.
/// </summary>
public static class CoreAutomationIds
{
    /// <summary>
    /// Shell-bezogene IDs.
    /// </summary>
    public static class Shell
    {
        /// <summary>
        /// Root-Container der Shell.
        /// XAML: Shell.xaml - Border Element
        /// </summary>
        public const string Root = "Shell.Root";

        /// <summary>
        /// Splash Screen.
        /// XAML: Shell.xaml - ExtendedSplashScreen Element
        /// </summary>
        public const string SplashScreen = "Shell.SplashScreen";

        /// <summary>
        /// Loading-Indikator.
        /// XAML: Shell.xaml - ProgressRing Element
        /// </summary>
        public const string LoadingIndicator = "Shell.LoadingIndicator";
    }

    /// <summary>
    /// MainPage-bezogene IDs.
    /// </summary>
    public static class MainPage
    {
        /// <summary>
        /// Root-Container der MainPage.
        /// XAML: MainPage.xaml - Aeusseres Grid Element
        /// </summary>
        public const string Root = "MainPage.Root";

        /// <summary>
        /// Navigation Header.
        /// XAML: MainPage.xaml - NavigationBar Element
        /// </summary>
        public const string NavigationBar = "MainPage.NavigationBar";

        /// <summary>
        /// Headline Text.
        /// XAML: MainPage.xaml - Erstes TextBlock "Welcome to Heimatplatz!"
        /// </summary>
        public const string HeadlineText = "MainPage.HeadlineText";

        /// <summary>
        /// Subtitle Text.
        /// XAML: MainPage.xaml - Zweites TextBlock mit Subtitle
        /// </summary>
        public const string SubtitleText = "MainPage.SubtitleText";

        /// <summary>
        /// Click Me Button.
        /// XAML: MainPage.xaml - Button mit ClickCommand
        /// </summary>
        public const string ClickButton = "MainPage.ClickButton";

        /// <summary>
        /// Click Counter Text.
        /// XAML: MainPage.xaml - TextBlock mit ClickCount Binding
        /// </summary>
        public const string ClickCountText = "MainPage.ClickCountText";

        /// <summary>
        /// Busy Overlay.
        /// XAML: MainPage.xaml - BusyOverlay Control
        /// </summary>
        public const string BusyOverlay = "MainPage.BusyOverlay";
    }

    /// <summary>
    /// Navigation-bezogene IDs.
    /// </summary>
    public static class Navigation
    {
        /// <summary>
        /// Zurueck-Button in NavigationBar.
        /// </summary>
        public const string BackButton = "Navigation.BackButton";

        /// <summary>
        /// Hamburger Menu Button.
        /// </summary>
        public const string MenuButton = "Navigation.MenuButton";
    }

    /// <summary>
    /// Allgemeine UI-Elemente.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Generischer Loading-Indikator.
        /// </summary>
        public const string LoadingIndicator = "Common.LoadingIndicator";

        /// <summary>
        /// Error-Anzeige Container.
        /// </summary>
        public const string ErrorContainer = "Common.ErrorContainer";

        /// <summary>
        /// Error-Nachricht Text.
        /// </summary>
        public const string ErrorMessage = "Common.ErrorMessage";
    }
}
