using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressDescription",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Phone",
                table: "Addresses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Upzila",
                table: "Addresses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressDescription",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Upzila",
                table: "Addresses");
        }
    }
}
