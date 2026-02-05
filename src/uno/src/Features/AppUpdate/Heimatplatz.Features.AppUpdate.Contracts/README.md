# Heimatplatz.Features.AppUpdate.Contracts

Contract definitions for the Android In-App Updates feature.

## Overview

This project contains interfaces and models for managing in-app updates on Android devices.

## Contents

- `IAppUpdateService` - Main service interface for checking and installing updates
- `Models/AppUpdateInfo` - Information about available updates
- `Models/AppUpdateOptions` - Configuration options
- `Models/UpdateDownloadProgress` - Download progress information
- `Models/UpdateResult` - Result of an update operation

## Usage

Inject `IAppUpdateService` to interact with the update system:

```csharp
public class MyViewModel
{
    private readonly IAppUpdateService _updateService;

    public MyViewModel(IAppUpdateService updateService)
    {
        _updateService = updateService;
    }

    public async Task CheckForUpdatesAsync()
    {
        var info = await _updateService.CheckForUpdateAsync();
        if (info?.IsUpdateAvailable == true)
        {
            await _updateService.StartFlexibleUpdateAsync();
        }
    }
}
```
