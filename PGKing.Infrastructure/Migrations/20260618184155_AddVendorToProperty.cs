using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "Properties",
                type: "int",
                nullable: true);



            migrationBuilder.CreateIndex(
                name: "IX_Properties_VendorId",
                table: "Properties",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Vendors_VendorId",
                table: "Properties",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Vendors_VendorId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_VendorId",
                table: "Properties");



            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "Properties");
        }
    }
}
