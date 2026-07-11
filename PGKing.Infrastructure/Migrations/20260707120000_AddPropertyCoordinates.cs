using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Properties",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Properties",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Properties");
        }
    }
}
