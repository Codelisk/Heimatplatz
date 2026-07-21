using Shiny.Mediator;

namespace Heimatplatz.Api.Features.WkoCompanies.Contracts.Mediator.Requests;

public record TriggerWkoCompanySyncRequest : IRequest<TriggerWkoCompanySyncResponse>;

public record TriggerWkoCompanySyncResponse
{
    public int Created { get; init; }
    public int Updated { get; init; }
    public int Removed { get; init; }
    public int Unchanged { get; init; }
    public int Errors { get; init; }
    public List<string> ErrorMessages { get; init; } = [];
}
