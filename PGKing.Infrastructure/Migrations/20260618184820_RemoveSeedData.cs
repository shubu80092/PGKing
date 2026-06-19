using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGKing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "TenantId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Vendors",
                keyColumn: "VendorId",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "VendorId", "CompanyName", "ContactPerson", "CreatedBy", "CreatedDate", "Email", "IsActive", "MobileNumber", "ModifiedBy", "ModifiedDate", "PasswordHash" },
                values: new object[] { 1, "Test Vendor Company", "John Vendor", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "vendor@test.com", true, "9876543210", null, null, "vendor123" });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "TenantId", "CompanyName", "ContactPerson", "CreatedBy", "CreatedDate", "Email", "IsActive", "MobileNumber", "ModifiedBy", "ModifiedDate", "PasswordHash", "VendorId" },
                values: new object[] { 1, "Test Tenant Company", "Alice Tenant", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "tenant@test.com", true, "1234567890", null, null, "tenant123", 1 });
        }
    }
}
