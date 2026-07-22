using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Feedback.Data.Configurations;

public class FeedbackAttachmentConfiguration : IEntityTypeConfiguration<FeedbackAttachment>
{
    public void Configure(EntityTypeBuilder<FeedbackAttachment> builder)
    {
        builder.ToTable("FeedbackAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Kind).IsRequired();

        builder.HasOne(x => x.Message)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MessageId);
    }
}
