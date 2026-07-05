using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptsReprintPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "Category", "Code", "Description" },
                values: new object[] { new Guid("00000000-0000-0000-0002-000000000016"), 0, "receipts.reprint", "Reprint a receipt after the fact (audited)." });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000001") },
                    { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000002") },
                    { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000003") },
                    { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000004") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000002") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000003") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0002-000000000016"), new Guid("00000000-0000-0000-0001-000000000004") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000016"));
        }
    }
}
