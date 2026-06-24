using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintAttachmentConfiguration : IEntityTypeConfiguration<ComplaintAttachment>
{
    public void Configure(EntityTypeBuilder<ComplaintAttachment> builder)
    {
        builder.ToTable("complaint_attachments", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.ComplaintId)
            .HasColumnName("complaint_id")
            .IsRequired();

        builder.Property(entity => entity.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(entity => entity.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.SizeInBytes)
            .HasColumnName("size_in_bytes")
            .IsRequired();

        builder.Property(entity => entity.StorageProvider)
            .HasColumnName("storage_provider")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(entity => entity.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id");

        builder.OwnsOne(entity => entity.PhotoExifLocation, location =>
        {
            location.Property(coordinate => coordinate.Latitude)
                .HasColumnName("photo_exif_latitude");

            location.Property(coordinate => coordinate.Longitude)
                .HasColumnName("photo_exif_longitude");
        });

        builder.Property(entity => entity.PhotoExifGeometry)
            .HasColumnName("photo_exif_geometry")
            .HasColumnType("geometry(Point,4326)")
            .HasConversion(
                value => GeometryConversion.ToNullablePoint(value),
                value => GeometryConversion.ToNullableWkt(value));

        builder.HasIndex(entity => entity.ComplaintId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
