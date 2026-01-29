using System.Text.Json;
using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Data.Entities;

/// <summary>
/// Property entity
/// </summary>
public class Property : BaseEntity
{
    /// <summary>Property title/heading</summary>
    public required string Title { get; set; }

    /// <summary>Street address and house number</summary>
    public required string Address { get; set; }

    /// <summary>City</summary>
    public required string City { get; set; }

    /// <summary>Postal/ZIP code</summary>
    public required string PostalCode { get; set; }

    /// <summary>Purchase price in currency</summary>
    public decimal Price { get; set; }

    /// <summary>Living area in m² (null for land)</summary>
    public int? LivingAreaSquareMeters { get; set; }

    /// <summary>Plot/land area in m²</summary>
    public int? PlotAreaSquareMeters { get; set; }

    /// <summary>Number of rooms (null for land)</summary>
    public int? Rooms { get; set; }

    /// <summary>Year built (null for land)</summary>
    public int? YearBuilt { get; set; }

    /// <summary>Type of property</summary>
    public PropertyType Type { get; set; }

    /// <summary>Type of seller</summary>
    public SellerType SellerType { get; set; }

    /// <summary>Name of the seller</summary>
    public required string SellerName { get; set; }

    /// <summary>Description text</summary>
    public string? Description { get; set; }

    /// <summary>Features/amenities (JSON array)</summary>
    public List<string> Features { get; set; } = [];

    /// <summary>Image URLs (JSON array)</summary>
    public List<string> ImageUrls { get; set; } = [];

    /// <summary>Type-specific attributes (JSON)</summary>
    public string TypeSpecificData { get; set; } = "{}";

    /// <summary>ID of the user (seller) who created this property</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the user (seller)</summary>
    public User User { get; set; } = null!;

    /// <summary>Wie funktioniert die Anfrage zu dieser Immobilie</summary>
    public InquiryType InquiryType { get; set; } = InquiryType.ContactData;

    /// <summary>Kontaktinformationen zu dieser Immobilie</summary>
    public List<PropertyContactInfo> Contacts { get; set; } = [];

    // === Import-Tracking Felder ===

    /// <summary>Name des Quellsystems (z.B. "ImmoScout24", "Willhaben")</summary>
    public string? SourceName { get; set; }

    /// <summary>Externe ID im Quellsystem</summary>
    public string? SourceId { get; set; }

    /// <summary>Original-URL beim Quellsystem</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Letzte Aktualisierung beim Quellsystem</summary>
    public DateTimeOffset? SourceLastUpdated { get; set; }

    /// <summary>
    /// Gets the type-specific data deserialized to the specified type
    /// </summary>
    /// <typeparam name="T">The type to deserialize to (LandPropertyData, HousePropertyData, or ForeclosurePropertyData)</typeparam>
    /// <returns>The deserialized type-specific data, or null if the data is empty or invalid</returns>
    public T? GetTypedData<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(TypeSpecificData) || TypeSpecificData == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(TypeSpecificData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the type-specific data by serializing the provided object to JSON
    /// </summary>
    /// <typeparam name="T">The type to serialize (LandPropertyData, HousePropertyData, or ForeclosurePropertyData)</typeparam>
    /// <param name="data">The data to serialize and store</param>
    public void SetTypedData<T>(T data) where T : class
    {
        TypeSpecificData = JsonSerializer.Serialize(data);
    }
}
