using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_municipality_id_code",
                schema: "public",
                table: "departments");

            migrationBuilder.AlterColumn<Guid>(
                name: "municipality_id",
                schema: "public",
                table: "departments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "institution_id",
                schema: "public",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_institution_id_code",
                schema: "public",
                table: "departments",
                columns: new[] { "institution_id", "code" },
                unique: true,
                filter: "institution_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_departments_municipality_id_code",
                schema: "public",
                table: "departments",
                columns: new[] { "municipality_id", "code" },
                unique: true,
                filter: "municipality_id IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_institution_id_code",
                schema: "public",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_municipality_id_code",
                schema: "public",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "institution_id",
                schema: "public",
                table: "departments");

            migrationBuilder.AlterColumn<Guid>(
                name: "municipality_id",
                schema: "public",
                table: "departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_municipality_id_code",
                schema: "public",
                table: "departments",
                columns: new[] { "municipality_id", "code" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
