using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PinHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FailedPinAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedOutUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_members_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_members_organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_members_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_members_users_LinkedUserId",
                        column: x => x.LinkedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_member_roles",
                columns: table => new
                {
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_member_roles", x => new { x.StaffMemberId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_staff_member_roles_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_member_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_member_roles_staff_members_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_member_roles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_StaffMemberId",
                table: "auth_sessions",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_member_roles_LocationId",
                table: "staff_member_roles",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_member_roles_RoleId",
                table: "staff_member_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_member_roles_TenantId",
                table: "staff_member_roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_LinkedUserId",
                table: "staff_members",
                column: "LinkedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_LocationId",
                table: "staff_members",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_OrganisationId_StaffCode",
                table: "staff_members",
                columns: new[] { "OrganisationId", "StaffCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_TenantId",
                table: "staff_members",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_auth_sessions_staff_members_StaffMemberId",
                table: "auth_sessions",
                column: "StaffMemberId",
                principalTable: "staff_members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_auth_sessions_staff_members_StaffMemberId",
                table: "auth_sessions");

            migrationBuilder.DropTable(
                name: "staff_member_roles");

            migrationBuilder.DropTable(
                name: "staff_members");

            migrationBuilder.DropIndex(
                name: "IX_auth_sessions_StaffMemberId",
                table: "auth_sessions");
        }
    }
}
