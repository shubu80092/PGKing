using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260723000000_AddTestimonials")]
    public partial class AddTestimonials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `Testimonials` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(100) NOT NULL,
    `Designation` VARCHAR(100) NULL,
    `Message` VARCHAR(1000) NOT NULL,
    `ImageUrl` LONGTEXT NULL,
    `Rating` INT NOT NULL DEFAULT 5,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `DisplayOrder` INT NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
");

            // Insert some seed data
            migrationBuilder.Sql(@"
INSERT INTO `Testimonials` (`Id`, `Name`, `Designation`, `Message`, `ImageUrl`, `Rating`, `IsActive`, `DisplayOrder`) VALUES
(1, 'Aarti Sharma', 'Software Engineer', 'PGKing completely changed how I live. The amenities are top-notch and the community is incredible. Highly recommended for working professionals!', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400', 5, 1, 1),
(2, 'Vikas Patel', 'MBA Student', 'The best part about PGKing is the hassle-free living. Everything is taken care of, from cleaning to high-speed internet. It feels just like home.', 'https://images.unsplash.com/photo-1599566150163-29194dcaad36?w=400', 5, 1, 2),
(3, 'Neha Singh', 'Freelance Designer', 'Safe, secure, and beautiful spaces. The co-working areas are perfect for someone like me who works from home often.', 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400', 4, 1, 3)
ON DUPLICATE KEY UPDATE `Name`=VALUES(`Name`);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `Testimonials`;");
        }
    }
}
