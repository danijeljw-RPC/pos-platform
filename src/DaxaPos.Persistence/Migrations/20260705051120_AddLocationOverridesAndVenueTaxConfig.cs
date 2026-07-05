using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationOverridesAndVenueTaxConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_location_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSoldOut = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PriceOverride = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_location_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_location_overrides_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_location_overrides_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_location_overrides_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "venue_tax_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxInclusivePricing = table.Column<bool>(type: "boolean", nullable: false),
                    TaxCalculationMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venue_tax_configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_venue_tax_configurations_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_venue_tax_configurations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_location_overrides_LocationId_ProductId",
                table: "product_location_overrides",
                columns: new[] { "LocationId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_location_overrides_ProductId",
                table: "product_location_overrides",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_location_overrides_TenantId",
                table: "product_location_overrides",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_venue_tax_configurations_LocationId",
                table: "venue_tax_configurations",
                column: "LocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_venue_tax_configurations_TenantId",
                table: "venue_tax_configurations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_location_overrides");

            migrationBuilder.DropTable(
                name: "venue_tax_configurations");
        }
    }
}
