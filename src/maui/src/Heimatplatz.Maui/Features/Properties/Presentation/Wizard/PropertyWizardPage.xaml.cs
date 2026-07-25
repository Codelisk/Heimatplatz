namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

public partial class PropertyWizardPage : ContentPage
{
    private PropertyWizardViewModel? _viewModel;

    public PropertyWizardPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.Ort.PropertyChanged -= OnOrtPropertyChanged;

        _viewModel = BindingContext as PropertyWizardViewModel;
        if (_viewModel != null)
            _viewModel.Ort.PropertyChanged += OnOrtPropertyChanged;
    }

    /// <summary>
    /// Die Ort-Vorschlaege erscheinen unter der Adresszeile - liegt die gerade am
    /// unteren Rand, verdeckt die fixe Veroeffentlichen-Leiste die Liste. Beim
    /// Auftauchen die Vorschlaege deshalb in den sichtbaren Bereich scrollen.
    /// </summary>
    private void OnOrtPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Models.MunicipalitySearchModel.HasOrtSuggestions) ||
            _viewModel?.Ort.HasOrtSuggestions != true)
            return;

        // Nach dem Layout-Pass, sonst hat das frisch eingeblendete Panel noch Hoehe 0
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await WizardScroll.ScrollToAsync(OrtSuggestionsPanel, ScrollToPosition.Center, animated: true);
            }
            catch (Exception)
            {
                // Layout noch nicht bereit - der Nutzer kann weiterhin manuell scrollen
            }
        });
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
