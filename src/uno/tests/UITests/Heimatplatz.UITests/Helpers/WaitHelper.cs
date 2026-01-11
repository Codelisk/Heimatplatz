using Heimatplatz.UITests.Configuration;
using Uno.UITest;

namespace Heimatplatz.UITests.Helpers;

/// <summary>
/// Helper fuer explizite Waits auf UI-Elemente.
/// </summary>
public class WaitHelper
{
    private readonly IApp _app;

    public WaitHelper(IApp app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    /// <summary>
    /// Wartet auf ein Element mit dem angegebenen AutomationId.
    /// </summary>
    public void ForElement(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        _app.WaitForElement(automationId, timeout: wait);
    }

    /// <summary>
    /// Wartet bis ein Element nicht mehr sichtbar ist.
    /// </summary>
    public void ForNoElement(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        _app.WaitForNoElement(automationId, timeout: wait);
    }

    /// <summary>
    /// Wartet auf ein Element mit dem angegebenen Text.
    /// </summary>
    public void ForText(string text, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        _app.WaitForElement(c => c.Text(text), timeout: wait);
    }

    /// <summary>
    /// Wartet bis eine Bedingung erfuellt ist.
    /// </summary>
    public void Until(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? pollingInterval = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        var polling = pollingInterval ?? TimeSpan.FromMilliseconds(250);
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < wait)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(polling);
        }

        throw new TimeoutException($"Bedingung wurde nicht innerhalb von {wait.TotalSeconds} Sekunden erfuellt.");
    }

    /// <summary>
    /// Wartet bis ein Element einen bestimmten Text hat.
    /// </summary>
    public void ForElementWithText(string automationId, string expectedText, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;

        Until(() =>
        {
            var elements = _app.Query(automationId);
            return elements.Any(e => e.Text == expectedText);
        }, wait);
    }

    /// <summary>
    /// Wartet bis ein Element aktiviert ist.
    /// </summary>
    public void ForElementEnabled(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;

        Until(() =>
        {
            var elements = _app.Query(automationId);
            return elements.Any(e => e.Enabled);
        }, wait);
    }

    /// <summary>
    /// Wartet bis ein Element deaktiviert ist.
    /// </summary>
    public void ForElementDisabled(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;

        Until(() =>
        {
            var elements = _app.Query(automationId);
            return elements.Any(e => !e.Enabled);
        }, wait);
    }

    /// <summary>
    /// Wartet eine feste Zeitspanne (nur wenn unbedingt noetig!).
    /// </summary>
    public void ForSeconds(double seconds)
    {
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Wartet auf mehrere Elemente gleichzeitig.
    /// </summary>
    public void ForAllElements(TimeSpan? timeout = null, params string[] automationIds)
    {
        var wait = timeout ?? Constants.DefaultTimeout;

        foreach (var id in automationIds)
        {
            _app.WaitForElement(id, timeout: wait);
        }
    }

    /// <summary>
    /// Wartet auf eines von mehreren Elementen.
    /// </summary>
    public string ForAnyElement(TimeSpan? timeout = null, params string[] automationIds)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < wait)
        {
            foreach (var id in automationIds)
            {
                if (_app.Query(id).Any())
                {
                    return id;
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Keines der Elemente [{string.Join(", ", automationIds)}] wurde gefunden.");
    }
}
