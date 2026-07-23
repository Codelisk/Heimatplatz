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
                decor, new ImeFollowCallback(composerView, decor));
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
    /// Layout-Durchlauf. Die ContentPage respektiert mit SafeAreaEdges="Container"
    /// bereits Navigationsleiste und Display-Cutout. Android liefert diesen bereits von
    /// MAUI verbrauchten Container-Inset waehrend der IME-Animation auf manchen Samsung-
    /// Geraeten als 0. Deshalb lesen wir den stabilen Navigationsleisten-Inset direkt
    /// von den unbeschnittenen RootWindowInsets und ziehen ihn vom IME-Inset ab;
    /// andernfalls entstuende unter der Eingabe genau dieser Abstand ein zweites Mal.
    /// DispatchModeContinueOnSubtree laesst andere Inset-Verbraucher (MAUI) unberuehrt.
    /// </summary>
    private sealed class ImeFollowCallback
        : AndroidX.Core.View.WindowInsetsAnimationCompat.Callback
    {
        private readonly Android.Views.View _composerView;
        private readonly Android.Views.View _decorView;
        private int _containerBottom;
        private bool _hasContainerBottom;

        public ImeFollowCallback(
            Android.Views.View composerView,
            Android.Views.View decorView)
            : base(DispatchModeContinueOnSubtree)
        {
            _composerView = composerView;
            _decorView = decorView;
            CaptureContainerBottom();
        }

        public override void OnPrepare(
            AndroidX.Core.View.WindowInsetsAnimationCompat? animation)
        {
            if (animation != null
                && (animation.TypeMask
                    & AndroidX.Core.View.WindowInsetsCompat.Type.Ime()) != 0
                && Math.Abs(_composerView.TranslationY) < 0.5f)
            {
                CaptureContainerBottom();
            }

            base.OnPrepare(animation);
        }

        public override AndroidX.Core.View.WindowInsetsCompat OnProgress(
            AndroidX.Core.View.WindowInsetsCompat? insets,
            System.Collections.Generic.IList<AndroidX.Core.View.WindowInsetsAnimationCompat>? runningAnimations)
        {
            if (insets != null)
            {
                if (!_hasContainerBottom && Math.Abs(_composerView.TranslationY) < 0.5f)
                    CaptureContainerBottom();

                var imeBottom = insets
                    .GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;
                var keyboardOffset = Math.Max(0, imeBottom - _containerBottom);
                _composerView.TranslationY = -keyboardOffset;
            }

            return insets ?? AndroidX.Core.View.WindowInsetsCompat.Consumed!;
        }

        private void CaptureContainerBottom()
        {
            var rootInsets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(_decorView);
            if (rootInsets == null)
                return;

            _containerBottom = rootInsets.GetInsetsIgnoringVisibility(
                AndroidX.Core.View.WindowInsetsCompat.Type.NavigationBars()
                | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout())?.Bottom ?? 0;
            _hasContainerBottom = true;
        }
    }
#endif
}
