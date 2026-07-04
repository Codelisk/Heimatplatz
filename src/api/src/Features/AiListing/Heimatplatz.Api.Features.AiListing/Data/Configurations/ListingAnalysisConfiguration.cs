using Heimatplatz.Api.Features.AiListing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.AiListing.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer ListingAnalysis Entity
/// </summary>
public class ListingAnalysisConfiguration : IEntityTypeConfiguration<ListingAnalysis>
{
    public void Configure(EntityTypeBuilder<ListingAnalysis> builder)
    {
        builder.ToTable("ListingAnalyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.DictatedText)
            .HasMaxLength(8000);

        builder.Property(a => a.UserNotes)
            .HasMaxLength(4000);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(4000);

        // JSON-Columns fuer Listen (gleiches Muster wie Property.ImageUrls)
        builder.Property(a => a.ImageUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(a => a.VideoUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(a => a.ResultJson)
            .HasColumnType("TEXT");

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Status);
    }
}
