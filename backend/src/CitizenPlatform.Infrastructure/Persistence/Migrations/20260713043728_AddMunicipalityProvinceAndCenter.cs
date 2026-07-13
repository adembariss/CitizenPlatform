using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipalityProvinceAndCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "center_latitude",
                schema: "public",
                table: "municipalities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "center_longitude",
                schema: "public",
                table: "municipalities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "public",
                table: "municipalities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "center_latitude",
                schema: "public",
                table: "municipalities");

            migrationBuilder.DropColumn(
                name: "center_longitude",
                schema: "public",
                table: "municipalities");

            migrationBuilder.DropColumn(
                name: "province",
                schema: "public",
                table: "municipalities");
        }
    }
}
