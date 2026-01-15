namespace Heimatplatz.App.Presentation;

public partial class MainViewModel(BaseServices baseServices) : PageViewModel(baseServices)
{
    [ObservableProperty]
    private string _title = "Heimatplatz";

    [ObservableProperty]
    private int _clickCount;

    [UnoCommand]
    private async Task ClickAsync()
    {
        using (BeginBusy("Processing..."))
        {
            await Task.Delay(500);
            ClickCount++;
        }
    }
}
