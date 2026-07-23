using Heimatplatz.Maui.Features.Feedback.Models;

namespace Heimatplatz.Maui.Features.Feedback.Controls;

/// <summary>
/// Eingabezeile im Messenger-Stil, unten an der Seite angedockt. Der <see cref="Composer"/>
/// haelt Text, Bilder und Sprachnachricht; <see cref="SendCommand"/> kommt von der
/// jeweiligen Seite (neue Anfrage bzw. Antwort im Verlauf).
///
/// Tastatur (Android): das Fenster steht auf AdjustNothing (ComposerSoftInput), damit
/// weder System-Pan noch MAUI-Resize die Zeile bewegen. Stattdessen faehrt ein
/// WindowInsetsAnimation-Callback die Zeile per TranslationY Bild fuer Bild mit der
/// Tastatur mit - reine Render-Transformation ohne Layout-Durchlauf, dadurch fluessig
/// (AdjustResize/MAUI-SafeArea bewegten sie erst am Animationsende in einem Ruck).
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
    private Android.Views.View? _decorView;

    // Der Callback MUSS an der DecorView haengen, nicht an der Composer-View selbst:
    // MAUI faengt die WindowInsets weiter oben ab, sodass ein Callback auf der
    // Composer-View gar nicht mehr feuert. Die DecorView ist die Wurzel - dort laeuft
    // die IME-Animation immer durch. Uebersetzt wird trotzdem nur die Composer-View.
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        var decor = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;

        if (Handler?.PlatformView is Android.Views.View composerView && decor != null)
        {
            _decorView = decor;
            AndroidX.Core.View.ViewCompat.SetWindowInsetsAnimationCallback(
                decor, new ImeFollowCallback(composerView));
        }
        else if (_decorView != null)
        {
            AndroidX.Core.View.ViewCompat.SetWindowInsetsAnimationCallback(_decorView, null);
            _decorView = null;
        }
    }

    /// <summary>
    /// Bewegt die Eingabezeile per TranslationY synchron zur Tastatur-Animation.
    /// Der IME-Inset laeuft beim Ein-/Ausblenden weich von 0 auf volle Hoehe (bzw.
    /// zurueck), das spiegeln wir direkt in die Verschiebung - Bild fuer Bild, ohne
    /// Layout-Durchlauf. Verschoben wird um den vollen IME-Inset (nicht abzueglich
    /// Navigationsleiste): die Composer-View reicht bis zum Fensterrand, ihr Unterrand
    /// landet damit exakt auf der Tastatur-Oberkante. DispatchModeContinueOnSubtree,
    /// damit andere Inset-Verbraucher (MAUI) unberuehrt bleiben.
    /// </summary>
    private sealed class ImeFollowCallback(Android.Views.View composerView)
        : AndroidX.Core.View.WindowInsetsAnimationCompat.Callback(DispatchModeContinueOnSubtree)
    {
        public override AndroidX.Core.View.WindowInsetsCompat OnProgress(
            AndroidX.Core.View.WindowInsetsCompat? insets,
            System.Collections.Generic.IList<AndroidX.Core.View.WindowInsetsAnimationCompat>? runningAnimations)
        {
            var ime = insets?.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime());
            if (ime != null)
                composerView.TranslationY = -ime.Bottom;

            return insets ?? AndroidX.Core.View.WindowInsetsCompat.Consumed!;
        }
    }
#endif
}
