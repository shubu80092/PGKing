using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260801160000_AddGalleryItems")]
    public partial class AddGalleryItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `GalleryItems` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Title` VARCHAR(150) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `MediaType` VARCHAR(20) NOT NULL DEFAULT 'Photo',
    `Category` VARCHAR(50) NOT NULL DEFAULT 'Rooms',
    `MediaUrl` VARCHAR(1000) NOT NULL,
    `ThumbnailUrl` VARCHAR(1000) NULL,
    `DisplayOrder` INT NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
");

            migrationBuilder.Sql(@"
INSERT INTO `GalleryItems` (`Id`, `Title`, `Description`, `MediaType`, `Category`, `MediaUrl`, `ThumbnailUrl`, `DisplayOrder`, `IsActive`, `CreatedAt`) VALUES
(1, 'Luxury Premium Living Lounge', 'Spacious air-conditioned common room with comfortable seating and ambient decor.', 'Photo', 'Rooms', 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1000', NULL, 1, 1, '2026-08-01 10:00:00'),
(2, 'Executive Single Suite', 'Ergonomic study desk, luxury bedding, and high-speed Wi-Fi access.', 'Photo', 'Rooms', 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=1000', NULL, 2, 1, '2026-08-01 10:01:00'),
(3, 'Annual PGKing Student Fest & Music Night', 'Highlights from our community evening and live music celebration.', 'Video', 'Events', 'https://www.youtube.com/embed/dQw4w9WgXcQ', 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=1000', 3, 1, '2026-08-01 10:02:00'),
(4, 'Community Gaming & Lounge Area', 'Pool table, PlayStation setups, and relaxing recliners for weekend fun.', 'Photo', 'Community', 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=1000', NULL, 4, 1, '2026-08-01 10:03:00'),
(5, 'Hygienic Multi-Cuisine Dining Hall', 'Freshly prepared nutritious meals served 3 times a day in a spotless cafe.', 'Photo', 'Dining', 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1000', NULL, 5, 1, '2026-08-01 10:04:00'),
(6, '24x7 High-Speed Wi-Fi & Study Zone', 'Quiet co-working and reading spaces designed for productivity.', 'Photo', 'Amenities', 'https://images.unsplash.com/photo-1497366216548-37526070297c?w=1000', NULL, 6, 1, '2026-08-01 10:05:00'),
(7, 'Virtual Walkthrough of Boys Co-living Campus', 'Take a 360-degree tour of our flagship accommodation property in Noida.', 'Video', 'Rooms', 'https://www.youtube.com/embed/ScMzIvxBSi4', 'https://images.unsplash.com/photo-1513694203232-719a280e022f?w=1000', 7, 1, '2026-08-01 10:06:00'),
(8, 'Spacious Balcony & Evening View', 'Sunset view from our top-floor terrace garden with seating.', 'Photo', 'Community', 'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=1000', NULL, 8, 1, '2026-08-01 10:07:00')
ON DUPLICATE KEY UPDATE `Title`=VALUES(`Title`), `MediaUrl`=VALUES(`MediaUrl`);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `GalleryItems`;");
        }
    }
}
