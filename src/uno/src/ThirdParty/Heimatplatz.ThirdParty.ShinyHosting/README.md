# Heimatplatz.ThirdParty.ShinyHosting

Shiny Framework Hosting Integration for Uno Platform - equivalent to `Shiny.Hosting.Maui`.

## Purpose

Bridges Shiny 4.0 framework with Uno Platform's hosting model by:
- Initializing `ShinyHost` with the service provider
- Forwarding Android lifecycle events to Shiny's `AndroidShinyHost`

## Usage

### 1. Add Service Registration

In your `ConfigureServices`:

```csharp
services.AddShinyUno();
```

### 2. Android: Initialize in Application Class

In `Platforms/Android/Main.Android.cs`:

```csharp
using Shiny;

[Application(...)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
        // Initialize Shiny Android host
        ShinyUnoExtensions.InitializeShinyAndroid(this);
    }
}
```

### 3. Android: Forward Lifecycle Events in MainActivity

In `Platforms/Android/MainActivity.Android.cs`:

```csharp
using Shiny;

public class MainActivity : ...
{
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        ShinyUnoExtensions.OnRequestPermissionsResult(this, requestCode, permissions, grantResults);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        ShinyUnoExtensions.OnActivityResult(this, requestCode, resultCode, data);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        ShinyUnoExtensions.OnNewIntent(this, intent);
    }
}
```

## Dependencies

- `Shiny.Core` 4.0.0-beta

## Why This Exists

Shiny 4.0 beta's `PushManager.RequestAccess()` requires proper Android activity lifecycle tracking.
Without this integration, you get: "A current activity was not detected to be able to request permissions"
