using Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.PropertyDrafts.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer PropertyDraft Entity
/// </summary>
public class PropertyDraftConfiguration : IEntityTypeConfiguration<PropertyDraft>
{
    public void Configure(EntityTypeBuilder<PropertyDraft> builder)
    {
        builder.ToTable("PropertyDrafts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.Property(d => d.Title)
            .HasMaxLength(200);

        builder.Property(d => d.FirstImageUrl)
            .HasMaxLength(1000);

        builder.Property(d => d.PayloadJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(d => d.DescriptionKeywords)
            .HasMaxLength(2000);

        builder.Property(d => d.GeneratedDescription)
            .HasColumnType("TEXT");

        builder.Property(d => d.DescriptionError)
            .HasMaxLength(2000);

        builder.HasIndex(d => d.UserId);
    }
}
