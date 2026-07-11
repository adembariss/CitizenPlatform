using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAndAdminComplaintFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                schema: "public",
                table: "complaints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible_to_citizen",
                schema: "public",
                table: "complaint_status_histories",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "closed_at",
                schema: "public",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "is_visible_to_citizen",
                schema: "public",
                table: "complaint_status_histories");
        }
    }
}
