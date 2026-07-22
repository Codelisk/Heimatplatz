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

        // Die Zeile hebt sich NUR ueber die Tastatur, wenn das eigene Nachrichtenfeld
        // den Fokus hat - beim Tippen in anderen Feldern (z.B. Betreff) bleibt sie
        // unten, sonst springt das Layout bei jedem Fokuswechsel hin und her.
        MessageEditor.Focused += (_, _) =>
        {
            EditorFocused?.Invoke(this, EventArgs.Empty);
            OnEditorFocusChanged(true);
        };
        MessageEditor.Unfocused += (_, _) => OnEditorFocusChanged(false);
    }

    /// <summary>Feuert, wenn das Nachrichtenfeld den Fokus bekommt (Seiten scrollen dann passend).</summary>
    public event EventHandler? EditorFocused;

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
    private bool _editorFocused;

    private void OnEditorFocusChanged(bool focused)
    {
        _editorFocused = focused;
        ApplyKeyboardMargin();
    }

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

    /// <summary>Aktuellen IME-Inset lesen und die Zeile heben/senken (nur bei eigenem Fokus).</summary>
    private void ApplyKeyboardMargin()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            return;

        var decor = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;
        if (decor == null)
            return;

        SetKeyboardMargin(ReadImeDp(decor));
    }

    private static double ReadImeDp(Android.Views.View decor)
    {
        var insets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor);
        if (insets == null)
            return 0;

        // Voller IME-Inset: die Zeile soll direkt auf der Tastatur-Oberkante sitzen
        var ime = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;
        var density = decor.Resources?.DisplayMetrics?.Density ?? 1f;
        return Math.Max(0, ime) / density;
    }

    private void SetKeyboardMargin(double imeDp)
    {
        var bottomDp = _editorFocused ? imeDp : 0;
        if (Math.Abs(Margin.Bottom - bottomDp) > 0.5)
            Margin = new Thickness(0, 0, 0, bottomDp);
    }

    private sealed class KeyboardWatcher(MessageComposer owner, Android.Views.View decor)
        : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnGlobalLayoutListener
    {
        public void OnGlobalLayout()
        {
            var imeDp = ReadImeDp(decor);
            MainThread.BeginInvokeOnMainThread(() => owner.SetKeyboardMargin(imeDp));
        }
    }
#else
    private static void OnEditorFocusChanged(bool focused)
    {
        // iOS/Windows verschieben Eingaben selbst ueber die Tastatur - nichts zu tun
    }
#endif
}
