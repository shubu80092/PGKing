using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBasicClientInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OccupiedByAadhar",
                table: "PGRooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OccupiedByAddress",
                table: "PGRooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OccupiedByEmail",
                table: "PGRooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OccupiedByEmergencyContact",
                table: "PGRooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccupiedByAadhar",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "OccupiedByAddress",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "OccupiedByEmail",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "OccupiedByEmergencyContact",
                table: "PGRooms");
        }
    }
}
