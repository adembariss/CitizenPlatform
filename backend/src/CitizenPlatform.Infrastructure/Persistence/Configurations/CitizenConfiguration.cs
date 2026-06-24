using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class CitizenConfiguration : IEntityTypeConfiguration<Citizen>
{
    public void Configure(EntityTypeBuilder<Citizen> builder)
    {
        builder.ToTable("citizens", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id");

        builder.Property(entity => entity.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(40);

        builder.Property(entity => entity.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.HasIndex(entity => entity.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL AND is_deleted = false");

        builder.HasIndex(entity => entity.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL AND is_deleted = false");

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Citizen>(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
