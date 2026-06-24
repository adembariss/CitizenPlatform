using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationAttemptConfiguration : IEntityTypeConfiguration<IntegrationAttempt>
{
    public void Configure(EntityTypeBuilder<IntegrationAttempt> builder)
    {
        builder.ToTable("integration_attempts", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.OutboxMessageId)
            .HasColumnName("outbox_message_id")
            .IsRequired();

        builder.Property(entity => entity.AttemptNumber)
            .HasColumnName("attempt_number")
            .IsRequired();

        builder.Property(entity => entity.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(entity => entity.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.Succeeded)
            .HasColumnName("succeeded");

        builder.Property(entity => entity.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(4000);

        builder.HasIndex(entity => new { entity.OutboxMessageId, entity.AttemptNumber })
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
