using System.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class EditPropertyPage : ContentPage
{
    private EditPropertyViewModel? _subscribedVm;

    public EditPropertyPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;

        _subscribedVm = BindingContext as EditPropertyViewModel;
        if (_subscribedVm != null)
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Das Fehler-Banner sitzt oben im Formular, der Speichern-Button unten -
    /// bei einem Validierungsfehler nach oben scrollen, damit er sichtbar wird.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditPropertyViewModel.HasError) && _subscribedVm?.HasError == true)
            MainThread.BeginInvokeOnMainThread(() => _ = FormScroll.ScrollToAsync(0, 0, animated: true));
    }
}
