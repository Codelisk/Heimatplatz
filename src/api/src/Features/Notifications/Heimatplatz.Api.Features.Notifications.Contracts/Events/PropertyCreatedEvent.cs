using Heimatplatz.Api.Features.Properties.Contracts;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Events;

/// <summary>
/// Event published when a new property is created
/// </summary>
/// <param name="PropertyId">ID of the created property</param>
/// <param name="Title">Title of the property</param>
/// <param name="City">City/location of the property</param>
/// <param name="Price">Price of the property</param>
/// <param name="Type">Type of the property (House, Land, Foreclosure)</param>
/// <param name="SellerType">Type of the seller (Private, Broker, Portal)</param>
public record PropertyCreatedEvent(
    Guid PropertyId,
    string Title,
    string City,
    decimal Price,
    PropertyType Type,
    SellerType SellerType
) : IEvent;
