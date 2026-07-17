using CitizenPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CitizenPlatform.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints", "public");
        builder.ConfigureAuditableEntity();

        builder.Property(entity => entity.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(entity => entity.InstitutionId)
            .HasColumnName("institution_id");

        builder.Property(entity => entity.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(entity => entity.CitizenId)
            .HasColumnName("citizen_id");

        builder.Property(entity => entity.TrackingCode)
            .HasColumnName("tracking_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasColumnName("description")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(entity => entity.AddressText)
            .HasColumnName("address_text")
            .HasMaxLength(1000);

        builder.OwnsOne(entity => entity.Location, location =>
        {
            location.Property(coordinate => coordinate.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            location.Property(coordinate => coordinate.Longitude)
                .HasColumnName("longitude")
                .IsRequired();
        });

        builder.Navigation(entity => entity.Location)
            .IsRequired();

        builder.Property(entity => entity.LocationGeometry)
            .HasColumnName("location_geometry")
            .HasColumnType("geometry(Point,4326)")
            .HasConversion(
                value => GeometryConversion.ToPoint(value),
                value => GeometryConversion.ToWkt(value))
            .IsRequired();

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

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.CurrentDepartmentId)
            .HasColumnName("current_department_id");

        builder.Property(entity => entity.AssignedUserId)
            .HasColumnName("assigned_user_id");

        builder.Property(entity => entity.ExternalMunicipalityComplaintId)
            .HasColumnName("external_municipality_complaint_id")
            .HasMaxLength(128);

        builder.Property(entity => entity.ExternalMunicipalityStatus)
            .HasColumnName("external_municipality_status")
            .HasMaxLength(100);

        builder.Property(entity => entity.LastSyncAttemptAt)
            .HasColumnName("last_sync_attempt_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.SyncedAt)
            .HasColumnName("synced_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.ClosedAt)
            .HasColumnName("closed_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(entity => entity.TrackingCode)
            .IsUnique()
            .HasDatabaseName("ux_complaints_tracking_code");

        builder.HasIndex(entity => entity.LocationGeometry)
            .HasMethod("gist")
            .HasDatabaseName("ix_complaints_location_geometry_gist");

        builder.HasIndex(entity => new { entity.MunicipalityId, entity.Status });
        builder.HasIndex(entity => new { entity.MunicipalityId, entity.CategoryId });
        builder.HasIndex(entity => new { entity.InstitutionId, entity.Status });
        builder.HasIndex(entity => entity.CitizenId)
            .HasFilter("citizen_id IS NOT NULL");

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(entity => entity.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ComplaintCategory>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Citizen>()
            .WithMany()
            .HasForeignKey(entity => entity.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entity => entity.CurrentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => entity.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Attachments)
            .WithOne()
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.StatusHistories)
            .WithOne()
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.StatusHistories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Comments)
            .WithOne()
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.Assignments)
            .WithOne()
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(entity => entity.Assignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
