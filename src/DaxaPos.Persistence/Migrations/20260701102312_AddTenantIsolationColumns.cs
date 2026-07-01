using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "terminals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "locations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "devices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_terminals_TenantId",
                table: "terminals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_TenantId",
                table: "locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_devices_TenantId",
                table: "devices",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_devices_tenants_TenantId",
                table: "devices",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_locations_tenants_TenantId",
                table: "locations",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_terminals_tenants_TenantId",
                table: "terminals",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_devices_tenants_TenantId",
                table: "devices");

            migrationBuilder.DropForeignKey(
                name: "FK_locations_tenants_TenantId",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "FK_terminals_tenants_TenantId",
                table: "terminals");

            migrationBuilder.DropIndex(
                name: "IX_terminals_TenantId",
                table: "terminals");

            migrationBuilder.DropIndex(
                name: "IX_locations_TenantId",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "IX_devices_TenantId",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "terminals");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "devices");
        }
    }
}
