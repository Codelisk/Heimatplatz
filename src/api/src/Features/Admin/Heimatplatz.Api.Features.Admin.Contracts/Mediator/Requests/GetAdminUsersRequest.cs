using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Admin.Contracts.Mediator.Requests;

/// <summary>
/// Nutzerliste fuer den Intern-Bereich - neueste Registrierungen zuerst.
/// Search matcht case-insensitive auf Name, E-Mail und Firmenname.
/// </summary>
public record GetAdminUsersRequest(
    int Page = 0,
    int PageSize = 50,
    string? Search = null
) : IRequest<GetAdminUsersResponse>;

public record GetAdminUsersResponse(
    List<AdminUserDto> Users,
    int Total,
    int PageSize,
    int CurrentPage,
    bool HasMore
);

public record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    SellerType? SellerType,
    string? CompanyName,
    bool IsAdmin,
    bool EmailVerified,
    DateTimeOffset CreatedAt,
    int PropertyCount
);
