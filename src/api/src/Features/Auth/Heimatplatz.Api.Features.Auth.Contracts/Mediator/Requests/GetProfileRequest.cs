using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen des eigenen Profils (Benutzer kommt aus dem JWT)
/// </summary>
public record GetProfileRequest : IRequest<GetProfileResponse>;

/// <summary>
/// Response mit den Profildaten des authentifizierten Benutzers
/// </summary>
// Nullable Felder mit Default, damit der OpenAPI-Generator sie nicht als "required"
// markiert (der MAUI-Client wuerde sonst non-nullable Typen generieren)
public record GetProfileResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string FullName,
    SellerType? SellerType = null,
    string? CompanyName = null,
    bool IsAdmin = false
);
