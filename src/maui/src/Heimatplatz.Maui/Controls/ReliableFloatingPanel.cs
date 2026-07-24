using ShinyFloatingPanel = Shiny.Maui.Controls.FloatingPanel.FloatingPanel;

namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Haelt den angeforderten Panelzustand ueber eine laufende Shiny-Animation hinweg
/// fest. FloatingPanel ignoriert Zustandswechsel waehrend seiner Animation; ein
/// schneller Tap auf "Fertig" oder eine Auswahl konnte deshalb ein sichtbar
/// offenes Panel mit IsOpen=false zuruecklassen.
/// </summary>
public sealed class ReliableFloatingPanel : ShinyFloatingPanel
{
    public static readonly BindableProperty RequestedIsOpenProperty = BindableProperty.Create(
        nameof(RequestedIsOpen),
        typeof(bool),
        typeof(ReliableFloatingPanel),
        false,
        BindingMode.TwoWay,
        propertyChanged: static (bindable, _, _) =>
            ((ReliableFloatingPanel)bindable).ReconcileRequestedState());

    private bool _visualIsOpen;
    private bool _transitionInProgress;
    private bool _applyingRequestedState;
    private bool _synchronizingRequestedState;

    public ReliableFloatingPanel()
    {
        Opened += OnOpened;
        Closed += OnClosed;
    }

    public bool RequestedIsOpen
    {
        get => (bool)GetValue(RequestedIsOpenProperty);
        set => SetValue(RequestedIsOpenProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Backdrop, Drag-Geste und Header koennen den Basiszustand direkt aendern.
        // Diesen Zustand auch zur ViewModel-Bindung zurueckspiegeln.
        if (propertyName != nameof(IsOpen) || _applyingRequestedState)
            return;

        _transitionInProgress = true;
        _synchronizingRequestedState = true;
        SetValue(RequestedIsOpenProperty, IsOpen);
        _synchronizingRequestedState = false;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _visualIsOpen = true;
        _transitionInProgress = false;
        ReconcileRequestedState();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _visualIsOpen = false;
        _transitionInProgress = false;
        ReconcileRequestedState();
    }

    private void ReconcileRequestedState()
    {
        if (_synchronizingRequestedState ||
            _transitionInProgress ||
            RequestedIsOpen == _visualIsOpen)
        {
            return;
        }

        _transitionInProgress = true;
        _applyingRequestedState = true;
        IsOpen = RequestedIsOpen;
        _applyingRequestedState = false;
    }
}
