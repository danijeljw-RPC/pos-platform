using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTerminalDeviceIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_terminals_DeviceId",
                table: "terminals");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_DeviceId",
                table: "terminals",
                column: "DeviceId",
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_terminals_DeviceId",
                table: "terminals");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_DeviceId",
                table: "terminals",
                column: "DeviceId");
        }
    }
}
