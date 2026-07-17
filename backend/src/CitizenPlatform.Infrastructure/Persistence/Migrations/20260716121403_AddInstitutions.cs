using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "institutions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    center_latitude = table.Column<double>(type: "double precision", nullable: true),
                    center_longitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "institution_service_areas",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institution_service_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_institution_service_areas_institutions_institution_id",
                        column: x => x.institution_id,
                        principalSchema: "public",
                        principalTable: "institutions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_institution_service_areas_institution_id",
                schema: "public",
                table: "institution_service_areas",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "IX_institution_service_areas_is_deleted",
                schema: "public",
                table: "institution_service_areas",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_institution_service_areas_province_district",
                schema: "public",
                table: "institution_service_areas",
                columns: new[] { "province", "district" });

            migrationBuilder.CreateIndex(
                name: "IX_institutions_code",
                schema: "public",
                table: "institutions",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_institutions_is_deleted",
                schema: "public",
                table: "institutions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_institutions_type",
                schema: "public",
                table: "institutions",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "institution_service_areas",
                schema: "public");

            migrationBuilder.DropTable(
                name: "institutions",
                schema: "public");
        }
    }
}
