namespace Heimatplatz.App.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public ContentControl ContentControl => Splash;

    public Frame NavigationFrame => RootFrame;

    public Shell()
    {
        this.InitializeComponent();
    }
}
