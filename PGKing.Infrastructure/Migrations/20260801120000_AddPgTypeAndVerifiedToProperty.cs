using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PGKing.Infrastructure.Data;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260801120000_AddPgTypeAndVerifiedToProperty")]
    public partial class AddPgTypeAndVerifiedToProperty : Migration
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

            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'PgType', \"VARCHAR(50) NOT NULL DEFAULT 'Co-living'\");");
            migrationBuilder.Sql("CALL AddColumnIfNotExists('Properties', 'IsVerified', 'TINYINT(1) NOT NULL DEFAULT 1');");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS AddColumnIfNotExists;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PgType",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Properties");
        }
    }
}
