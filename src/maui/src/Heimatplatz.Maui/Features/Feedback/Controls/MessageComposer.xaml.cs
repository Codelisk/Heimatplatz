using Heimatplatz.Maui.Features.Feedback.Models;

namespace Heimatplatz.Maui.Features.Feedback.Controls;

/// <summary>
/// Eingabezeile im Messenger-Stil. Der <see cref="Composer"/> haelt Text, Bilder und
/// Sprachnachricht; <see cref="SendCommand"/> kommt von der jeweiligen Seite
/// (neue Anfrage bzw. Antwort im Verlauf).
/// </summary>
public partial class MessageComposer : ContentView
{
    public static readonly BindableProperty ComposerProperty = BindableProperty.Create(
        nameof(Composer),
        typeof(FeedbackComposer),
        typeof(MessageComposer),
        propertyChanged: OnComposerChanged);

    public static readonly BindableProperty SendCommandProperty = BindableProperty.Create(
        nameof(SendCommand),
        typeof(System.Windows.Input.ICommand),
        typeof(MessageComposer));

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(MessageComposer),
        string.Empty);

    public MessageComposer()
    {
        InitializeComponent();
    }

    public FeedbackComposer? Composer
    {
        get => (FeedbackComposer?)GetValue(ComposerProperty);
        set => SetValue(ComposerProperty, value);
    }

    public System.Windows.Input.ICommand? SendCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(SendCommandProperty);
        set => SetValue(SendCommandProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // Der innere Baum bindet direkt gegen den Composer (x:DataType), damit die
    // Vorlagen ohne RelativeSource-Umwege auskommen
    private static void OnComposerChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MessageComposer view)
            view.ComposerRoot.BindingContext = newValue;
    }

#if ANDROID
    private KeyboardWatcher? _keyboardWatcher;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Ab Android 15 (API 35, Edge-to-Edge erzwungen) ignoriert das System
        // adjustResize - die Tastatur wuerde die Zeile verdecken. Wir lesen die
        // IME-Insets deshalb selbst von den Root-Insets des Fensters (GlobalLayout
        // feuert bei jedem Tastatur-Ein/Ausblenden) und heben die Zeile per Margin.
        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            return;

        var decor = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;
        if (decor == null)
            return;

        if (Handler != null && _keyboardWatcher == null)
        {
            _keyboardWatcher = new KeyboardWatcher(this, decor);
            decor.ViewTreeObserver?.AddOnGlobalLayoutListener(_keyboardWatcher);
        }
        else if (Handler == null && _keyboardWatcher != null)
        {
            decor.ViewTreeObserver?.RemoveOnGlobalLayoutListener(_keyboardWatcher);
            _keyboardWatcher = null;
        }
    }

    private sealed class KeyboardWatcher(MessageComposer owner, Android.Views.View decor)
        : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnGlobalLayoutListener
    {
        public void OnGlobalLayout()
        {
            var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
            if (insets == null)
                return;

            // Voller IME-Inset: die Zeile soll direkt auf der Tastatur-Oberkante sitzen
            var ime = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;
            var density = decor.Resources?.DisplayMetrics?.Density ?? 1f;
            var bottomDp = Math.Max(0, ime) / density;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Math.Abs(owner.Margin.Bottom - bottomDp) > 0.5)
                    owner.Margin = new Thickness(0, 0, 0, bottomDp);
            });
        }
    }
#endif
}
