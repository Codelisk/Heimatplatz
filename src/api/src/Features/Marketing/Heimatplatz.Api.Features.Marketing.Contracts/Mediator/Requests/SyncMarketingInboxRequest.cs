using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;

/// <summary>Manueller Posteingang-Abruf ("Jetzt abrufen") - umgeht die Sync-Drossel.</summary>
public record SyncMarketingInboxRequest : IRequest<SyncMarketingInboxResponse>;

public record SyncMarketingInboxResponse(bool Success, int Added, string? Error);
