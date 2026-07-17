using System.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

public partial class DescriptionStepView : ContentView
{
    private CancellationTokenSource? _pulseCts;
    private PropertyWizardViewModel? _viewModel;

    public DescriptionStepView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as PropertyWizardViewModel;

        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PropertyWizardViewModel.IsListening))
            return;

        if (_viewModel?.IsListening == true)
            StartPulse();
        else
            StopPulse();
    }

    private void StartPulse()
    {
        StopPulse();
        _pulseCts = new CancellationTokenSource();
        _ = PulseLoopAsync(_pulseCts.Token);
    }

    private void StopPulse()
    {
        _pulseCts?.Cancel();
        _pulseCts?.Dispose();
        _pulseCts = null;
    }

    /// <summary>
    /// Pulsierender Ring um den Mikrofon-Button waehrend des Diktats.
    /// </summary>
    private async Task PulseLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                PulseRing.Scale = 1;
                PulseRing.Opacity = 0.45;

                await Task.WhenAll(
                    PulseRing.ScaleToAsync(1.6, 900, Easing.SinOut),
                    PulseRing.FadeToAsync(0, 900, Easing.SinOut));

                await Task.Delay(200, ct);
            }
        }
        catch (TaskCanceledException)
        {
            // Diktat beendet
        }
        finally
        {
            PulseRing.Opacity = 0;
            PulseRing.Scale = 1;
        }
    }
}
