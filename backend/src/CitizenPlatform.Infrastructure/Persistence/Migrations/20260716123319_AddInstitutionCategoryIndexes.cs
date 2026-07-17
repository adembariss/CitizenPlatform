using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionCategoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_complaint_categories_code",
                schema: "public",
                table: "complaint_categories");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_categories_code",
                schema: "public",
                table: "complaint_categories",
                column: "code",
                unique: true,
                filter: "municipality_id IS NULL AND institution_id IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_categories_institution_id_code",
                schema: "public",
                table: "complaint_categories",
                columns: new[] { "institution_id", "code" },
                unique: true,
                filter: "institution_id IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_complaint_categories_code",
                schema: "public",
                table: "complaint_categories");

            migrationBuilder.DropIndex(
                name: "IX_complaint_categories_institution_id_code",
                schema: "public",
                table: "complaint_categories");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_categories_code",
                schema: "public",
                table: "complaint_categories",
                column: "code",
                unique: true,
                filter: "municipality_id IS NULL AND is_deleted = false");
        }
    }
}
