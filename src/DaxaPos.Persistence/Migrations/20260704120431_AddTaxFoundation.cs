using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DaxaPos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tax_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxTreatment = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_categories_organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_categories_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_definition_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RegionCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RatePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    JurisdictionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JurisdictionType = table.Column<int>(type: "integer", nullable: false),
                    IncludedInPrice = table.Column<bool>(type: "boolean", nullable: false),
                    RoundingMode = table.Column<int>(type: "integer", nullable: false),
                    RoundingPrecision = table.Column<int>(type: "integer", nullable: false),
                    CalculationScope = table.Column<int>(type: "integer", nullable: false),
                    ReceiptMarkerCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ReceiptMarkerLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReportingCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_definition_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tax_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RegionCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RatePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    JurisdictionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JurisdictionType = table.Column<int>(type: "integer", nullable: false),
                    IncludedInPrice = table.Column<bool>(type: "boolean", nullable: false),
                    RoundingMode = table.Column<int>(type: "integer", nullable: false),
                    RoundingPrecision = table.Column<int>(type: "integer", nullable: false),
                    CalculationScope = table.Column<int>(type: "integer", nullable: false),
                    ReceiptMarkerCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ReceiptMarkerLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReportingCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SourceTemplateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_definitions_organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_definitions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_category_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_category_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_category_definitions_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_category_definitions_tax_categories_TaxCategoryId",
                        column: x => x.TaxCategoryId,
                        principalTable: "tax_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_category_definitions_tax_definitions_TaxDefinitionId",
                        column: x => x.TaxDefinitionId,
                        principalTable: "tax_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_category_definitions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "tax_definition_templates",
                columns: new[] { "Id", "CalculationScope", "Code", "CountryCode", "IncludedInPrice", "IsActive", "JurisdictionName", "JurisdictionType", "Name", "RatePercent", "ReceiptMarkerCode", "ReceiptMarkerLabel", "RegionCode", "ReportingCategory", "RoundingMode", "RoundingPrecision" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000001"), 0, "AU_GST_10", "AU", true, true, "Australia", 0, "GST", 10m, null, null, null, "GST", 0, 2 },
                    { new Guid("00000000-0000-0000-0003-000000000002"), 0, "AU_GST_FREE", "AU", true, true, "Australia", 0, "GST", 0m, "F", "GST-free", null, "GSTFree", 0, 2 },
                    { new Guid("00000000-0000-0000-0003-000000000003"), 0, "NZ_GST_15", "NZ", true, true, "New Zealand", 0, "GST", 15m, null, null, null, "GST", 0, 2 },
                    { new Guid("00000000-0000-0000-0003-000000000004"), 0, "NZ_ZERO_RATED", "NZ", true, true, "New Zealand", 0, "GST", 0m, "Z", "Zero-rated", null, "ZeroRated", 0, 2 },
                    { new Guid("00000000-0000-0000-0003-000000000005"), 0, "NZ_EXEMPT", "NZ", true, true, "New Zealand", 0, "GST", 0m, "E", "Exempt", null, "Exempt", 0, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tax_categories_OrganisationId",
                table: "tax_categories",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_categories_TenantId",
                table: "tax_categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_categories_TenantId_Code",
                table: "tax_categories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_category_definitions_LocationId",
                table: "tax_category_definitions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_category_definitions_TaxCategoryId",
                table: "tax_category_definitions",
                column: "TaxCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_category_definitions_TaxDefinitionId",
                table: "tax_category_definitions",
                column: "TaxDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_category_definitions_TenantId",
                table: "tax_category_definitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_definition_templates_Code",
                table: "tax_definition_templates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_definitions_OrganisationId",
                table: "tax_definitions",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_definitions_TenantId",
                table: "tax_definitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_definitions_TenantId_Code",
                table: "tax_definitions",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_category_definitions");

            migrationBuilder.DropTable(
                name: "tax_definition_templates");

            migrationBuilder.DropTable(
                name: "tax_categories");

            migrationBuilder.DropTable(
                name: "tax_definitions");
        }
    }
}
