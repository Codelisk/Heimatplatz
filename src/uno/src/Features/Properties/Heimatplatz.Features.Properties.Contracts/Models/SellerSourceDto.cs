namespace Heimatplatz.Features.Properties.Contracts.Models;

/// <summary>
/// DTO for a seller source entry (broker company or portal platform)
/// </summary>
public record SellerSourceDto(
    Guid Id,
    string Name,
    SellerType SellerType
);
