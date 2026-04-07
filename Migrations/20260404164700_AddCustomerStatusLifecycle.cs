using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.Sql("UPDATE \"Customers\" SET \"Status\" = CASE WHEN \"IsActive\" THEN 'Active' ELSE 'Inactive' END WHERE \"Status\" IS NULL OR \"Status\" = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Customers");
        }
    }
}
