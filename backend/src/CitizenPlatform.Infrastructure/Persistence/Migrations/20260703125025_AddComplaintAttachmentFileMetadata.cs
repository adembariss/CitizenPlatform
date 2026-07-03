using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintAttachmentFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                schema: "public",
                table: "complaint_attachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "photo_taken_at",
                schema: "public",
                table: "complaint_attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sha256_hash",
                schema: "public",
                table: "complaint_attachments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "0000000000000000000000000000000000000000000000000000000000000000");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_attachments_sha256_hash",
                schema: "public",
                table: "complaint_attachments",
                column: "sha256_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_complaint_attachments_sha256_hash",
                schema: "public",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "original_file_name",
                schema: "public",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "photo_taken_at",
                schema: "public",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "sha256_hash",
                schema: "public",
                table: "complaint_attachments");
        }
    }
}
