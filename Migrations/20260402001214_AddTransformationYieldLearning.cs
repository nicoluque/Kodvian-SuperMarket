using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddTransformationYieldLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransformationYieldEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    SourceProductId = table.Column<int>(type: "integer", nullable: false),
                    TargetProductId = table.Column<int>(type: "integer", nullable: false),
                    SourceQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    TargetQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    YieldFactorObserved = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    SuggestedYieldFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    UsedSuggestedFactor = table.Column<bool>(type: "boolean", nullable: false),
                    DeviationPct = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                    SuggestionConfidence = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    SuggestionSampleCount = table.Column<int>(type: "integer", nullable: true),
                    SuggestionSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransformationYieldEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransformationYieldEvents_Products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldEvents_Products_TargetProductId",
                        column: x => x.TargetProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldEvents_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransformationYieldEvents_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransformationYieldProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    SourceProductId = table.Column<int>(type: "integer", nullable: false),
                    TargetProductId = table.Column<int>(type: "integer", nullable: false),
                    SuggestedYieldFactor = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    Confidence = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Volatility = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    LastRecalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransformationYieldProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransformationYieldProfiles_Products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldProfiles_Products_TargetProductId",
                        column: x => x.TargetProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransformationYieldProfiles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransformationYieldProfiles_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldEvents_AppliedAt",
                table: "TransformationYieldEvents",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldEvents_SourceProductId",
                table: "TransformationYieldEvents",
                column: "SourceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldEvents_StoreId_SupplierId_SourceProductI~",
                table: "TransformationYieldEvents",
                columns: new[] { "StoreId", "SupplierId", "SourceProductId", "TargetProductId", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldEvents_SupplierId",
                table: "TransformationYieldEvents",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldEvents_TargetProductId",
                table: "TransformationYieldEvents",
                column: "TargetProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldProfiles_LastRecalculatedAt",
                table: "TransformationYieldProfiles",
                column: "LastRecalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldProfiles_SourceProductId",
                table: "TransformationYieldProfiles",
                column: "SourceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldProfiles_StoreId_SupplierId_SourceProduc~",
                table: "TransformationYieldProfiles",
                columns: new[] { "StoreId", "SupplierId", "SourceProductId", "TargetProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldProfiles_SupplierId",
                table: "TransformationYieldProfiles",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TransformationYieldProfiles_TargetProductId",
                table: "TransformationYieldProfiles",
                column: "TargetProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransformationYieldEvents");

            migrationBuilder.DropTable(
                name: "TransformationYieldProfiles");
        }
    }
}
