namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// Mein Profil Seite
/// </summary>
public partial class UserProfilePage : ContentPage
{
    private UserProfileViewModel? _viewModel;
    private CancellationTokenSource? _themeToastCts;

    public UserProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.ThemeModeToastRequested -= OnThemeModeToastRequested;

        _viewModel = BindingContext as UserProfileViewModel;
        if (_viewModel != null)
            _viewModel.ThemeModeToastRequested += OnThemeModeToastRequested;
    }

    /// <summary>
    /// Blendet nach einem Tipp auf den Design-Umschalter kurz den Modusnamen ein.
    /// Schnelles Weitertippen bricht den laufenden Ablauf ab und startet ihn neu -
    /// die Pille bleibt dann stehen und zeigt den jeweils neuen Modus.
    /// </summary>
    private async void OnThemeModeToastRequested(object? sender, EventArgs e)
    {
        _themeToastCts?.Cancel();
        _themeToastCts?.Dispose();
        var cts = new CancellationTokenSource();
        _themeToastCts = cts;

        try
        {
            ThemeModeToast.IsVisible = true;
            await ThemeModeToast.FadeToAsync(1, 140, Easing.CubicOut);
            await Task.Delay(1200, cts.Token);
            await ThemeModeToast.FadeToAsync(0, 320, Easing.CubicIn);

            if (!cts.IsCancellationRequested)
                ThemeModeToast.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
            // Naechster Tipp uebernimmt die Pille
        }
    }
}
