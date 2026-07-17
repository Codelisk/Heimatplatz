using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Auth.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer UserActionToken Entity
/// </summary>
public class UserActionTokenConfiguration : IEntityTypeConfiguration<UserActionToken>
{
    public void Configure(EntityTypeBuilder<UserActionToken> builder)
    {
        builder.ToTable("UserActionTokens");

        builder.HasKey(t => t.Id);

        // SHA-256 als Hex = 64 Zeichen
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        // Lookup beim Einloesen erfolgt ueber den Hash
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // Aufraeumen/Ersetzen alter Tokens eines Benutzers pro Zweck
        builder.HasIndex(t => new { t.UserId, t.Purpose });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
