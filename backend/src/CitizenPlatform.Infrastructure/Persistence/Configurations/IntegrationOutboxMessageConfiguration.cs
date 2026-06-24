using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        builder.ToTable("integration_outbox", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(entity => entity.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(entity => entity.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(entity => entity.ProcessingStartedAt)
            .HasColumnName("processing_started_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.NextRetryAt)
            .HasColumnName("next_retry_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(4000);

        builder.HasIndex(entity => new { entity.Status, entity.NextRetryAt });
        builder.HasIndex(entity => new { entity.MunicipalityId, entity.AggregateId });

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Attempts)
            .WithOne()
            .HasForeignKey(entity => entity.OutboxMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Attempts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
