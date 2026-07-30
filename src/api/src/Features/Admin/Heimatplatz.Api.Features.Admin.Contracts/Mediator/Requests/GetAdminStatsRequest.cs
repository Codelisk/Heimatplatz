using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Kennzahlen fuer das Intern-Dashboard.
/// </summary>
public record GetAdminStatsRequest : IRequest<GetAdminStatsResponse>;

public record GetAdminStatsResponse(
    int TotalUsers,
    int NewUsers7Days,
    int NewUsers30Days,
    int TotalProperties,
    int UserProperties,
    int NewUserProperties7Days,
    int ForeclosureProperties,
    int HiddenProperties
);
