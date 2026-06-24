using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintStatusHistoryConfiguration : IEntityTypeConfiguration<ComplaintStatusHistory>
{
    public void Configure(EntityTypeBuilder<ComplaintStatusHistory> builder)
    {
        builder.ToTable("complaint_status_histories", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.ComplaintId)
            .HasColumnName("complaint_id")
            .IsRequired();

        builder.Property(entity => entity.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(entity => entity.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.ChangedByUserId)
            .HasColumnName("changed_by_user_id");

        builder.Property(entity => entity.Note)
            .HasColumnName("note")
            .HasMaxLength(4000);

        builder.HasIndex(entity => new { entity.ComplaintId, entity.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
