using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260802140000_AddSeoFieldsToProperty")]
    public partial class AddSeoFieldsToProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP PROCEDURE IF EXISTS AddColumnIfNotExists;
CREATE PROCEDURE AddColumnIfNotExists(
    IN tableName VARCHAR(255),
    IN columnName VARCHAR(255),
    IN columnDefinition TEXT
)
BEGIN
    DECLARE colExists INT DEFAULT 0;
    SELECT COUNT(*) INTO colExists
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = tableName
      AND COLUMN_NAME = columnName;
    
    IF colExists = 0 THEN
        SET @sqlstmt = CONCAT('ALTER TABLE `', tableName, '` ADD COLUMN `', columnName, '` ', columnDefinition);
        PREPARE stmt FROM @sqlstmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END;
");

            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'Area', 'VARCHAR(100) NULL');");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'CityName', 'VARCHAR(100) NULL');");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'StateName', 'VARCHAR(100) NULL');");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'PropertySlug', 'VARCHAR(200) NULL');");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'LocationSlug', 'VARCHAR(200) NULL');");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'CanonicalUrl', 'VARCHAR(500) NULL');");

            migrationBuilder.Sql(@"
DROP PROCEDURE IF EXISTS AddIndexIfNotExists;
CREATE PROCEDURE AddIndexIfNotExists(
    IN tableName VARCHAR(255),
    IN indexName VARCHAR(255),
    IN indexColumns VARCHAR(255)
)
BEGIN
    DECLARE idxExists INT DEFAULT 0;
    SELECT COUNT(*) INTO idxExists
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = tableName
      AND INDEX_NAME = indexName;
    
    IF idxExists = 0 THEN
        SET @sqlstmt = CONCAT('CREATE INDEX `', indexName, '` ON `', tableName, '` (', indexColumns, ')');
        PREPARE stmt FROM @sqlstmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END;
");

            migrationBuilder.Sql("CALL AddIndexIfNotExists('Properties', 'IX_Properties_LocationSlug_PropertySlug', '`LocationSlug`, `PropertySlug`');");

            migrationBuilder.Sql(@"
UPDATE `Properties` SET `Area` = 'Bhandup West' WHERE `Id` = 1 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Powai' WHERE `Id` = 2 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Andheri East' WHERE `Id` = 3 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Bhandup West' WHERE (`Area` IS NULL OR `Area` = '');

UPDATE `Properties` SET `CityName` = 'Mumbai' WHERE (`CityName` IS NULL OR `CityName` = '');
UPDATE `Properties` SET `StateName` = 'Maharashtra' WHERE (`StateName` IS NULL OR `StateName` = '');

UPDATE `Properties`
SET 
    `LocationSlug` = CONCAT('pg-in-', LOWER(REPLACE(REPLACE(TRIM(`Area`), ' ', '-'), '--', '-')), '-', LOWER(REPLACE(REPLACE(TRIM(`CityName`), ' ', '-'), '--', '-'))),
    `PropertySlug` = LOWER(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(`Title`), ' ', '-'), '.', ''), ',', ''), '--', '-'))
WHERE `PropertySlug` IS NULL OR `PropertySlug` = '';

UPDATE `Properties`
SET `CanonicalUrl` = CONCAT('https://pgking.in/', `LocationSlug`, '/', `PropertySlug`)
WHERE `CanonicalUrl` IS NULL OR `CanonicalUrl` = '';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP PROCEDURE IF EXISTS DropColumnIfExists;
CREATE PROCEDURE DropColumnIfExists(
    IN tableName VARCHAR(255),
    IN columnName VARCHAR(255)
)
BEGIN
    DECLARE colExists INT DEFAULT 0;
    SELECT COUNT(*) INTO colExists
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = tableName
      AND COLUMN_NAME = columnName;
    
    IF colExists > 0 THEN
        SET @sqlstmt = CONCAT('ALTER TABLE `', tableName, '` DROP COLUMN `', columnName, '`');
        PREPARE stmt FROM @sqlstmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END;
");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'CanonicalUrl');");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'LocationSlug');");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'PropertySlug');");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'StateName');");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'CityName');");
            migrationBuilder.Sql("CALL DropColumnIfExists('Properties', 'Area');");
        }
    }
}
