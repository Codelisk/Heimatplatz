using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;

/// <summary>
/// Laedt ein Partner-Logo ueber die bestehende Bild-Pipeline hoch (Original +
/// Display-Variante, selbst gehostet - kein Hotlink auf Partner-Domains, CSP).
/// Liefert die absolute URL fuer das LogoUrl-Feld. Admin-only.
/// </summary>
public record UploadPartnerLogoRequest(Base64ImageData Image) : IRequest<UploadPartnerLogoResponse>;

public record UploadPartnerLogoResponse(
    bool Success,
    string? Error,
    string? LogoUrl
);
