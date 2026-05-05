using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlatHierarchyFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlatId",
                table: "PGRooms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Flats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BhkType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flats_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FlatMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FlatId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlatMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlatMedias_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PGRooms_FlatId",
                table: "PGRooms",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_FlatMedias_FlatId",
                table: "FlatMedias",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_Flats_PropertyId",
                table: "Flats",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PGRooms_Flats_FlatId",
                table: "PGRooms",
                column: "FlatId",
                principalTable: "Flats",
                principalColumn: "Id");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PGRooms_Flats_FlatId",
                table: "PGRooms");

            migrationBuilder.DropTable(
                name: "FlatMedias");

            migrationBuilder.DropTable(
                name: "Flats");

            migrationBuilder.DropIndex(
                name: "IX_PGRooms_FlatId",
                table: "PGRooms");

            migrationBuilder.DropColumn(
                name: "FlatId",
                table: "PGRooms");

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "PGRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PGRooms_PropertyId",
                table: "PGRooms",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PGRooms_Properties_PropertyId",
                table: "PGRooms",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
