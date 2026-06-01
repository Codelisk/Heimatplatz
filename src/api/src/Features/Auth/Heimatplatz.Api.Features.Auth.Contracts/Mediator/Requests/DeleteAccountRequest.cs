using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Auth.Contracts.Mediator.Requests;

/// <summary>
/// Request zum endgueltigen Loeschen des eigenen Benutzerkontos inklusive aller
/// zugehoerigen Daten (Inserate, Favoriten, Blockierungen, Benachrichtigungen, Tokens, ...).
/// Der Benutzer wird serverseitig ueber das JWT (sub-Claim) identifiziert - es werden
/// keine Parameter benoetigt. Die Aktion ist nicht umkehrbar.
/// </summary>
public record DeleteAccountRequest : IRequest<DeleteAccountResponse>;

/// <summary>
/// Response nach erfolgreicher Konto-Loeschung.
/// </summary>
public record DeleteAccountResponse(bool Success);
