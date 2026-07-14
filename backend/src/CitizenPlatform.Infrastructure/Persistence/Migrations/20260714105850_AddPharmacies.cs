using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pharmacies",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    is_on_duty = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pharmacies_is_deleted",
                schema: "public",
                table: "pharmacies",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacies_is_on_duty",
                schema: "public",
                table: "pharmacies",
                column: "is_on_duty");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacies_province_district",
                schema: "public",
                table: "pharmacies",
                columns: new[] { "province", "district" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacies",
                schema: "public");
        }
    }
}
