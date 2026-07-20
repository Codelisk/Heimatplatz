using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Aktualisieren des eigenen Profils.
/// Damit kann ein Benutzer auch nachtraeglich Verkaeufer werden (SellerType setzen),
/// den Anbietertyp wechseln oder die Verkaeufer-Eigenschaft wieder ablegen (SellerType=null).
/// </summary>
public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    SellerType? SellerType = null,
    string? CompanyName = null,
    string? Phone = null
) : IRequest<UpdateProfileResponse>;

/// <summary>
/// Response nach Profil-Update. Enthaelt einen frischen Access Token,
/// damit geaenderte Rollen-Claims (Seller/SellerType) sofort wirken.
/// </summary>
// Nullable Felder mit Default, damit der OpenAPI-Generator sie nicht als "required"
// markiert (der MAUI-Client wuerde sonst non-nullable Typen generieren)
public record UpdateProfileResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string FullName,
    string AccessToken,
    SellerType? SellerType = null,
    string? CompanyName = null,
    string? Phone = null
);
