using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class ProductStockUniquePerStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductStocks_ProductId_Bucket",
                table: "ProductStocks");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_ProductId_Bucket",
                table: "ProductStocks",
                columns: new[] { "ProductId", "Bucket" },
                unique: true,
                filter: "\"StoreId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_ProductId_Bucket_StoreId",
                table: "ProductStocks",
                columns: new[] { "ProductId", "Bucket", "StoreId" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductStocks_ProductId_Bucket",
                table: "ProductStocks");

            migrationBuilder.DropIndex(
                name: "IX_ProductStocks_ProductId_Bucket_StoreId",
                table: "ProductStocks");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_ProductId_Bucket",
                table: "ProductStocks",
                columns: new[] { "ProductId", "Bucket" },
                unique: true);
        }
    }
}
