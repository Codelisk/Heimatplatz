using System.Windows.Input;

namespace Heimatplatz.Core.Controls.Controls;

public sealed partial class AppFooter : UserControl
{
    public static readonly DependencyProperty ImpressumCommandProperty =
        DependencyProperty.Register(nameof(ImpressumCommand), typeof(ICommand), typeof(AppFooter),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DatenschutzCommandProperty =
        DependencyProperty.Register(nameof(DatenschutzCommand), typeof(ICommand), typeof(AppFooter),
            new PropertyMetadata(null));

    public static readonly DependencyProperty KontaktCommandProperty =
        DependencyProperty.Register(nameof(KontaktCommand), typeof(ICommand), typeof(AppFooter),
            new PropertyMetadata(null));

    public ICommand? ImpressumCommand
    {
        get => (ICommand?)GetValue(ImpressumCommandProperty);
        set => SetValue(ImpressumCommandProperty, value);
    }

    public ICommand? DatenschutzCommand
    {
        get => (ICommand?)GetValue(DatenschutzCommandProperty);
        set => SetValue(DatenschutzCommandProperty, value);
    }

    public ICommand? KontaktCommand
    {
        get => (ICommand?)GetValue(KontaktCommandProperty);
        set => SetValue(KontaktCommandProperty, value);
    }

    public AppFooter()
    {
        this.InitializeComponent();
        ImpressumButton.Click += (s, e) => ImpressumCommand?.Execute(null);
        DatenschutzButton.Click += (s, e) => DatenschutzCommand?.Execute(null);
        KontaktButton.Click += (s, e) => KontaktCommand?.Execute(null);
    }
}
