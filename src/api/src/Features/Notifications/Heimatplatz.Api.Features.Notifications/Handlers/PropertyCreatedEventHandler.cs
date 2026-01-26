using Heimatplatz.Api;
using Heimatplatz.Api.Features.Notifications.Contracts.Events;
using Heimatplatz.Api.Features.Notifications.Services;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handles PropertyCreatedEvent and sends push notifications to matching users
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class PropertyCreatedEventHandler(
    IPushNotificationService pushNotificationService,
    ILogger<PropertyCreatedEventHandler> logger
) : IEventHandler<PropertyCreatedEvent>
{
    public async Task Handle(PropertyCreatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing PropertyCreatedEvent for property {PropertyId} in {City} (Type={Type}, SellerType={SellerType})",
            @event.PropertyId,
            @event.City,
            @event.Type,
            @event.SellerType);

        await pushNotificationService.SendPropertyNotificationAsync(
            @event.PropertyId,
            @event.Title,
            @event.City,
            @event.Price,
            @event.Type,
            @event.SellerType,
            cancellationToken);
    }
}
