# Heimatplatz.Api.Features.Notifications.Contracts

## Purpose
Provides shared contracts, events, and request/response DTOs for the Notifications feature.

## Contents

### Events
- **PropertyCreatedEvent**: Published when a new property is created, triggers location-based notifications

### Mediator Requests
- **GetNotificationPreferencesRequest**: Retrieves user's notification location preferences
- **UpdateNotificationPreferencesRequest**: Updates user's notification location filters
- **RegisterDeviceRequest**: Registers a device token for push notifications

## Dependencies
- Shiny.Mediator.Contracts

## Usage
Referenced by both the API Notifications feature and frontend for type-safe communication.
