using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id");

        builder.Property(entity => entity.CitizenId)
            .HasColumnName("citizen_id");

        builder.Property(entity => entity.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(entity => entity.Subject)
            .HasColumnName("subject")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Body)
            .HasColumnName("body")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(entity => entity.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(4000);

        builder.HasIndex(entity => new { entity.Status, entity.Channel });
        builder.HasIndex(entity => entity.UserId)
            .HasFilter("user_id IS NOT NULL");
        builder.HasIndex(entity => entity.CitizenId)
            .HasFilter("citizen_id IS NOT NULL");

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(entity => entity.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
