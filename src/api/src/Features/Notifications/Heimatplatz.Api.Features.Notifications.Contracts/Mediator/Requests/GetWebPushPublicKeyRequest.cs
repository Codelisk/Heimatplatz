using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request for the VAPID public key browsers need to create a Web Push subscription
/// </summary>
public record GetWebPushPublicKeyRequest : IRequest<GetWebPushPublicKeyResponse>;

/// <summary>
/// Response carrying the VAPID public key
/// </summary>
/// <param name="Enabled">Whether Web Push is configured on the server</param>
/// <param name="PublicKey">VAPID public key in base64url (web-push format), null when disabled</param>
public record GetWebPushPublicKeyResponse(bool Enabled, string? PublicKey);
