using System;
using Android.App;
using Android.Runtime;

namespace Heimatplatz.App.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
        // Shiny 4.0.0-beta doesn't expose AndroidShinyHost publicly
        // Activity tracking will be handled through AddShinyCoreServices()
    }
}
