using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceCredentialsAndRegistrationPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RotatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_credentials_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_credentials_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_registration_pins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PinHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: false),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_registration_pins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_registration_pins_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_registration_pins_organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_registration_pins_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_registration_pins_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_credentials_DeviceId",
                table: "device_credentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_device_credentials_TenantId",
                table: "device_credentials",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_device_registration_pins_CreatedAtUtc",
                table: "device_registration_pins",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_device_registration_pins_CreatedByUserId",
                table: "device_registration_pins",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_device_registration_pins_LocationId",
                table: "device_registration_pins",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_device_registration_pins_OrganisationId",
                table: "device_registration_pins",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_device_registration_pins_TenantId",
                table: "device_registration_pins",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_credentials");

            migrationBuilder.DropTable(
                name: "device_registration_pins");
        }
    }
}
