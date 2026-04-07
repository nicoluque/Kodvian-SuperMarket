using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierClaimSettlementPoliciesAndResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowClaimSettlementOverride",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClaimSettlementModeDefault",
                table: "Suppliers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedSettlementMode",
                table: "SupplierClaims",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "SupplierClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "SupplierClaims",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedSettlementMode",
                table: "SupplierClaims",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierClaimExchangeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierClaimId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierClaimExchangeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierClaimExchangeLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierClaimExchangeLines_SupplierClaims_SupplierClaimId",
                        column: x => x.SupplierClaimId,
                        principalTable: "SupplierClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierClaimRefunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierClaimId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierClaimRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierClaimRefunds_SupplierClaims_SupplierClaimId",
                        column: x => x.SupplierClaimId,
                        principalTable: "SupplierClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimExchangeLines_ProductId",
                table: "SupplierClaimExchangeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimExchangeLines_SupplierClaimId",
                table: "SupplierClaimExchangeLines",
                column: "SupplierClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimRefunds_CreatedAt",
                table: "SupplierClaimRefunds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimRefunds_SupplierClaimId",
                table: "SupplierClaimRefunds",
                column: "SupplierClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierClaimExchangeLines");

            migrationBuilder.DropTable(
                name: "SupplierClaimRefunds");

            migrationBuilder.DropColumn(
                name: "AllowClaimSettlementOverride",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ClaimSettlementModeDefault",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RequestedSettlementMode",
                table: "SupplierClaims");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "SupplierClaims");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "SupplierClaims");

            migrationBuilder.DropColumn(
                name: "ResolvedSettlementMode",
                table: "SupplierClaims");
        }
    }
}
