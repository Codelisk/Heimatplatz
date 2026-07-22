
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
}
