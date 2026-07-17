using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionTargetColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "institution_id",
                schema: "public",
                table: "user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "institution_id",
                schema: "public",
                table: "complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "institution_id",
                schema: "public",
                table: "complaint_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaints_institution_id_status",
                schema: "public",
                table: "complaints",
                columns: new[] { "institution_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_complaints_institution_id_status",
                schema: "public",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "institution_id",
                schema: "public",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "institution_id",
                schema: "public",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "institution_id",
                schema: "public",
                table: "complaint_categories");
        }
    }
}
