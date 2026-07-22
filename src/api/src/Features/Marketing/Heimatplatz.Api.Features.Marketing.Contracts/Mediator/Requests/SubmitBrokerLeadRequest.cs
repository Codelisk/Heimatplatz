using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>
/// Oeffentliche Makler-Anfrage von der /makler/-Seite des Webs (Concierge-Onboarding):
/// der Makler meldet sich, das Team legt Account an und pflegt die Inserate ein.
/// Landet als Kontakt (Typ Broker, Quelle "Makler-Anfrage") im Marketing-CRM.
/// Fax ist ein Honeypot gegen Spam-Bots: im Web unsichtbar - ausgefuellt wird die
/// Anfrage still verworfen (Success=true, damit der Bot nichts lernt).
/// </summary>
public record SubmitBrokerLeadRequest(
    string Email,
    string Company,
    string? ContactName,
    string? Phone,
    string? ListingsUrl,
    string? Message,
    string? Fax
) : IRequest<SubmitBrokerLeadResponse>;

public record SubmitBrokerLeadResponse(bool Success, string? Error);
