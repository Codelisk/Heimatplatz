namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO for a seller source entry (broker company)
/// </summary>
public record SellerSourceDto(
    Guid Id,
    string Name,
    SellerType SellerType
);
