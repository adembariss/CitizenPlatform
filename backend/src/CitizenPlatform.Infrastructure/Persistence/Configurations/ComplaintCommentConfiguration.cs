using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintCommentConfiguration : IEntityTypeConfiguration<ComplaintComment>
{
    public void Configure(EntityTypeBuilder<ComplaintComment> builder)
    {
        builder.ToTable("complaint_comments", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.ComplaintId)
            .HasColumnName("complaint_id")
            .IsRequired();

        builder.Property(entity => entity.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(entity => entity.Body)
            .HasColumnName("body")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(entity => entity.IsInternal)
            .HasColumnName("is_internal")
            .IsRequired();

        builder.HasIndex(entity => new { entity.ComplaintId, entity.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
