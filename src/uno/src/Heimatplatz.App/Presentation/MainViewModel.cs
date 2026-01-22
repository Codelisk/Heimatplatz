namespace Heimatplatz.App.Presentation;

/// <summary>
/// ViewModel for MainPage - Region Navigation Container
/// MainPage acts as a transparent container for region-based navigation.
/// Nested routes (Home, MyProperties, etc.) are displayed in the ContentRegion.
/// </summary>
public partial class MainViewModel(BaseServices baseServices) : PageViewModel(baseServices)
{
}
