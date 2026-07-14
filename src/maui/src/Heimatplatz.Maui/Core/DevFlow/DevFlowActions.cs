#if DEBUG
using System.ComponentModel;
using Heimatplatz.Maui.Features.Properties.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.DevFlow.Agent.Core;
using Shiny;

namespace Heimatplatz.Maui.Core.DevFlow;

/// <summary>
/// Debug-only DevFlow-Actions fuer UI-Automation (maui devflow extensions).
/// Noetig fuer parametrisierte Navigation: [ShellProperty]-Werte werden nur von den
/// generierten Navigationsmethoden gesetzt, "ui navigate" mit Query-String greift nicht.
/// </summary>
public static class DevFlowActions
{
    static IServiceProvider Services =>
        IPlatformApplication.Current?.Services
        ?? throw new InvalidOperationException("App noch nicht initialisiert");

    [DevFlowAction("navigate-property-detail", Description = "Oeffnet die PropertyDetailPage fuer die angegebene Immobilie")]
    public static Task NavigatePropertyDetail(
        [Description("Guid der Immobilie")] string propertyId) =>
        MainThread.InvokeOnMainThreadAsync(() =>
            Services.GetRequiredService<INavigator>()
                .NavigateTo<PropertyDetailViewModel>(vm => vm.PropertyId = propertyId));

    [DevFlowAction("navigate-foreclosure-detail", Description = "Oeffnet die ForeclosureDetailPage fuer die angegebene Zwangsversteigerung")]
    public static Task NavigateForeclosureDetail(
        [Description("Guid der Immobilie")] string propertyId) =>
        MainThread.InvokeOnMainThreadAsync(() =>
            Services.GetRequiredService<INavigator>()
                .NavigateTo<ForeclosureDetailViewModel>(vm => vm.PropertyId = propertyId));

    [DevFlowAction("navigate-edit-property", Description = "Oeffnet die EditPropertyPage fuer die angegebene eigene Immobilie")]
    public static Task NavigateEditProperty(
        [Description("Guid der Immobilie")] string propertyId) =>
        MainThread.InvokeOnMainThreadAsync(() =>
            Services.GetRequiredService<INavigator>()
                .NavigateTo<EditPropertyViewModel>(vm => vm.PropertyId = propertyId));

    [DevFlowAction("mock-home-properties", Description = "Laedt einen Debug-Datensatz in die Homepage, optional API-seitenweise")]
    public static Task MockHomeProperties(
        [Description("Anzahl der Mock-Immobilien")] int count = 3000,
        [Description("Nur die aktuelle API-Seite anzeigen")] bool usePagination = false) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            await GetHomeViewModel().LoadDebugMockPropertiesAsync(count, usePagination));

    [DevFlowAction("clear-home-property-mock", Description = "Entfernt den Debug-Datensatz und laedt wieder von der API")]
    public static Task ClearHomePropertyMock() =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            await GetHomeViewModel().ClearDebugMockPropertiesAsync());

    private static HomeViewModel GetHomeViewModel()
        => Shell.Current?.CurrentPage?.BindingContext as HomeViewModel
           ?? throw new InvalidOperationException("Die Homepage muss fuer diesen Test geoeffnet sein");
}
#endif
