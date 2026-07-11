using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260709000000_AddTeamMembers")]
    public partial class AddTeamMembers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `TeamMembers` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(100) NOT NULL,
    `Designation` VARCHAR(100) NOT NULL,
    `ImageUrl` LONGTEXT NULL,
    `Bio` LONGTEXT NULL,
    `LinkedInUrl` LONGTEXT NULL,
    `Email` LONGTEXT NULL,
    `DisplayOrder` INT NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
");

            migrationBuilder.Sql(@"
INSERT INTO `TeamMembers` (`Id`, `Name`, `Designation`, `Bio`, `ImageUrl`, `Email`, `DisplayOrder`, `IsActive`) VALUES
(1, 'Prahlad', 'Founder & CEO', 'Driving the vision to standardize premium, high-quality PG accommodations across India.', 'https://images.unsplash.com/photo-1560250097-0b93528c311a?w=600', 'info@pgking.in', 1, 1),
(2, 'Sneha Sharma', 'Head of Operations', 'Ensuring seamless property onboarding, regular quality maintenance, and tenant check-ins.', 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=600', 'info@pgking.in', 2, 1),
(3, 'Rahul Verma', 'Customer Relations', 'Dedicated to handling student and professional booking support, inquiries, and reviews.', 'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=600', 'info@pgking.in', 3, 1)
ON DUPLICATE KEY UPDATE `Name`=VALUES(`Name`);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `TeamMembers`;");
        }
    }
}
