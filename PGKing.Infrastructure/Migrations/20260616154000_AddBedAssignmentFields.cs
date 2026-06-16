using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    public partial class AddBedAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OccupiedByMobile",
                table: "PGRooms",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OccupiedByName",
                table: "PGRooms",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccupiedSince",
                table: "PGRooms",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccupiedByMobile",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "OccupiedByName",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "OccupiedSince",
                table: "PGRooms");
        }
    }
}
