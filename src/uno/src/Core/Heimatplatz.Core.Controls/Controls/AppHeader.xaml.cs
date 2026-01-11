using System.Windows.Input;

namespace Heimatplatz.Core.Controls.Controls;

public sealed partial class AppHeader : UserControl
{
    public static readonly DependencyProperty ObjectCountProperty =
        DependencyProperty.Register(nameof(ObjectCount), typeof(int), typeof(AppHeader),
            new PropertyMetadata(0, OnObjectCountChanged));

    public static readonly DependencyProperty AccountCommandProperty =
        DependencyProperty.Register(nameof(AccountCommand), typeof(ICommand), typeof(AppHeader),
            new PropertyMetadata(null));

    public int ObjectCount
    {
        get => (int)GetValue(ObjectCountProperty);
        set => SetValue(ObjectCountProperty, value);
    }

    public ICommand? AccountCommand
    {
        get => (ICommand?)GetValue(AccountCommandProperty);
        set => SetValue(AccountCommandProperty, value);
    }

    public AppHeader()
    {
        this.InitializeComponent();
        AccountButton.Click += (s, e) => AccountCommand?.Execute(null);
    }

    private static void OnObjectCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppHeader header)
        {
            header.ObjectCountText.Text = $"{e.NewValue:N0}";
        }
    }
}
