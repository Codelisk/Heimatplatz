using System.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class AddPropertyPage : ContentPage
{
    private AddPropertyViewModel? _subscribedVm;

    public AddPropertyPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribedVm = BindingContext as AddPropertyViewModel;
        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Das Fehler-Banner sitzt oben im Formular, der Speichern-Button unten -
    /// bei einem Validierungsfehler nach oben scrollen, damit er sichtbar wird.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddPropertyViewModel.HasError) && _subscribedVm?.HasError == true)
            MainThread.BeginInvokeOnMainThread(() => _ = FormScroll.ScrollToAsync(0, 0, animated: true));
    }
}
