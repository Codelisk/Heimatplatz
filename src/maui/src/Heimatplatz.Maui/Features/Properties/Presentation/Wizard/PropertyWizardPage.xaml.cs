namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

public partial class PropertyWizardPage : ContentPage
{
    public PropertyWizardPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Plattform-Zurueck auf den Abbruch-Prompt umleiten. Seit MAUI 10.0.90
    /// laeuft auch der iOS-Nav-Bar-Pfeil zuverlaessig ueber diesen Hook.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is PropertyWizardViewModel vm)
            return vm.HandleBackRequested();

        return base.OnBackButtonPressed();
    }
}
