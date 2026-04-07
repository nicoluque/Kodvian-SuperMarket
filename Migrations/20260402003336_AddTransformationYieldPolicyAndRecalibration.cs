using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddTransformationYieldPolicyAndRecalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransformationYieldRecalibrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    SourceProductId = table.Column<int>(type: "integer", nullable: false),
                    TargetProductId = table.Column<int>(type: "integer", nullable: false),
                    CurrentYieldFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    ProposedYieldFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    DeviationPct = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Volatility = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecisionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransformationYieldRecalibrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransformationYieldRecalibrationLogs_Products_SourceProduct~",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldRecalibrationLogs_Products_TargetProduct~",
                        column: x => x.TargetProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldRecalibrationLogs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransformationYieldRecalibrationLogs_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldRecalibrationLogs_CreatedAt",
                table: "TransformationYieldRecalibrationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldRecalibrationLogs_SourceProductId",
                table: "TransformationYieldRecalibrationLogs",
                column: "SourceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldRecalibrationLogs_StoreId_SupplierId_Sou~",
                table: "TransformationYieldRecalibrationLogs",
                columns: new[] { "StoreId", "SupplierId", "SourceProductId", "TargetProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldRecalibrationLogs_SupplierId",
                table: "TransformationYieldRecalibrationLogs",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldRecalibrationLogs_TargetProductId",
                table: "TransformationYieldRecalibrationLogs",
                column: "TargetProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransformationYieldRecalibrationLogs");
        }
    }
}
