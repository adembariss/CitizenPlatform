using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class CategoryDepartmentRuleConfiguration : IEntityTypeConfiguration<CategoryDepartmentRule>
{
    public void Configure(EntityTypeBuilder<CategoryDepartmentRule> builder)
    {
        builder.ToTable("category_department_rules", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(entity => entity.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(entity => entity.DefaultPriority)
            .HasColumnName("default_priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(entity => new { entity.MunicipalityId, entity.CategoryId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ComplaintCategory>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
