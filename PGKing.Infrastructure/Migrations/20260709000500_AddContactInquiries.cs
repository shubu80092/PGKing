using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709000500_AddContactInquiries")]
    public partial class AddContactInquiries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `ContactInquiries` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(100) NOT NULL,
    `Phone` VARCHAR(100) NOT NULL,
    `Email` VARCHAR(100) NOT NULL,
    `Message` LONGTEXT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `IsRead` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `ContactInquiries`;");
        }
    }
}
