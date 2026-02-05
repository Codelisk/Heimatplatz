# Heimatplatz.Features.AppUpdate

Android In-App Updates implementation for Heimatplatz using Shiny Mediator pattern.

## Overview

This feature enables automatic in-app updates on Android using Google Play Core. Users are prompted to update the app without leaving the application.

## Architecture

```
Heimatplatz.Features.AppUpdate/
├── Configuration/
│   └── ServiceCollectionExtensions.cs    # DI registration
├── Mediator/
│   └── Commands/
│       └── CheckForAppUpdateCommandHandler.cs
├── Platforms/
│   └── Android/
│       └── AndroidAppUpdateService.cs    # Play Core implementation
├── Services/
│   └── NoOpAppUpdateService.cs           # Stub for non-Android
└── README.md

Heimatplatz.Features.AppUpdate.Contracts/
├── IAppUpdateService.cs                  # Service interface
├── Mediator/
│   └── Commands/
│       └── CheckForAppUpdateCommand.cs   # Mediator command
└── Models/
    ├── AppUpdateInfo.cs
    ├── AppUpdateOptions.cs
    ├── UpdateDownloadProgress.cs
    └── UpdateResult.cs
```

## Usage

### Via Mediator (Recommended)

```csharp
// In App.xaml.cs or any service
var mediator = services.GetRequiredService<IMediator>();
await mediator.Send(new CheckForAppUpdateCommand());
```

### Direct Service Access

```csharp
var updateService = services.GetRequiredService<IAppUpdateService>();
var info = await updateService.CheckForUpdateAsync();

if (info?.IsUpdateAvailable == true)
{
    await updateService.StartFlexibleUpdateAsync();
}
```

## Update Types

### Flexible Updates (Default)
- Download happens in the background
- User can continue using the app
- Shows progress notification
- User chooses when to restart

### Immediate Updates
- Full-screen blocking UI
- App cannot be used until update completes
- Use for critical security updates (priority >= 4)

## Configuration

```csharp
updateService.Options = new AppUpdateOptions
{
    ImmediateUpdatePriority = 4,  // 0-5, updates >= this use immediate
    AllowAssetPackDeletion = false
};
```

## Testing

In-App Updates can only be tested:
- On real Android devices (not emulator)
- When app is installed from Google Play Store
- Internal testing track works for testing

## Platform Support

| Platform | Supported |
|----------|-----------|
| Android  | Yes       |
| iOS      | No (App Store handles updates) |
| WASM     | No (N/A)  |
| Desktop  | No (N/A)  |
