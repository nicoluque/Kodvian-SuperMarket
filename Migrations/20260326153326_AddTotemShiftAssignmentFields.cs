using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddTotemShiftAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_StoreId",
                table: "Sales");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedShiftBucket",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LateShiftOpen",
                table: "Sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShiftAssignedAt",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftAssignedByUsuarioId",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftAssignmentReason",
                table: "Sales",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftAssignmentStatus",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShiftBucket",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId_ShiftAssignmentStatus_CreatedAt",
                table: "Sales",
                columns: new[] { "StoreId", "ShiftAssignmentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId_ShiftBucket_CreatedAt",
                table: "Sales",
                columns: new[] { "StoreId", "ShiftBucket", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_StoreId_ShiftAssignmentStatus_CreatedAt",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_StoreId_ShiftBucket_CreatedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ExpectedShiftBucket",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "LateShiftOpen",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShiftAssignedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShiftAssignedByUsuarioId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShiftAssignmentReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShiftAssignmentStatus",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ShiftBucket",
                table: "Sales");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId",
                table: "Sales",
                column: "StoreId");
        }
    }
}
