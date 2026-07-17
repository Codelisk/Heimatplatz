namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

public partial class PropertyWizardPage : ContentPage
{
    public PropertyWizardPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Android-Hardware-Back und WinUI-Titelleisten-Back auf den Abbruch-Prompt
    /// umleiten (der iOS-Nav-Bar-Pfeil laeuft ueber Shell.BackButtonBehavior im XAML).
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is PropertyWizardViewModel vm)
            return vm.HandleBackRequested();

        return base.OnBackButtonPressed();
    }
}
