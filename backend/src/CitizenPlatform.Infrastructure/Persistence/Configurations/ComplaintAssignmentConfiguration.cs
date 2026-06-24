using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintAssignmentConfiguration : IEntityTypeConfiguration<ComplaintAssignment>
{
    public void Configure(EntityTypeBuilder<ComplaintAssignment> builder)
    {
        builder.ToTable("complaint_assignments", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.ComplaintId)
            .HasColumnName("complaint_id")
            .IsRequired();

        builder.Property(entity => entity.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(entity => entity.AssignedByUserId)
            .HasColumnName("assigned_by_user_id")
            .IsRequired();

        builder.Property(entity => entity.AssignedUserId)
            .HasColumnName("assigned_user_id");

        builder.Property(entity => entity.AssignedAt)
            .HasColumnName("assigned_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(entity => entity.Note)
            .HasColumnName("note")
            .HasMaxLength(4000);

        builder.HasIndex(entity => new { entity.ComplaintId, entity.AssignedAt });
        builder.HasIndex(entity => entity.DepartmentId);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
