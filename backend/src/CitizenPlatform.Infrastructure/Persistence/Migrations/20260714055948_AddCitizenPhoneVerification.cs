using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenPhoneVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phone_verification_code",
                schema: "public",
                table: "citizens",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "phone_verification_expires_at",
                schema: "public",
                table: "citizens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "phone_verified",
                schema: "public",
                table: "citizens",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phone_verification_code",
                schema: "public",
                table: "citizens");

            migrationBuilder.DropColumn(
                name: "phone_verification_expires_at",
                schema: "public",
                table: "citizens");

            migrationBuilder.DropColumn(
                name: "phone_verified",
                schema: "public",
                table: "citizens");
        }
    }
}
