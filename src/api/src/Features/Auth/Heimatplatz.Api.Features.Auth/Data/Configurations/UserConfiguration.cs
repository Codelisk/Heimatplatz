using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Auth.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer User Entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(UserInputValidator.MaxNameLength);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(UserInputValidator.MaxNameLength);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(UserInputValidator.MaxEmailLength);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.CompanyName)
            .HasMaxLength(UserInputValidator.MaxCompanyNameLength);

        builder.Property(u => u.Phone)
            .HasMaxLength(UserInputValidator.MaxPhoneLength);

        builder.Property(u => u.IsAdmin)
            .HasDefaultValue(false);

        // Eindeutiger Index auf Email
        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
