#if DEBUG
using UnoFramework.Pages;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// Test Push Page - Sendet Test-Push-Notifications (nur DEBUG)
/// </summary>
public sealed partial class TestPushPage : BasePage
{
    public TestPushViewModel ViewModel => (TestPushViewModel)DataContext;

    public TestPushPage()
    {
        this.InitializeComponent();
    }
}
#endif
