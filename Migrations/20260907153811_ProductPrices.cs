using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awai.Migrations
{
    /// <inheritdoc />
    public partial class ProductPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PriceNote",
                table: "Products",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444441"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 2500m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444442"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 1800m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444443"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 1200m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444444"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 1500m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444445"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 2000m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444446"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 3500m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444447"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 2200m, "يبدأ من / سنوياً" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d4d4d4d4-4444-4444-8444-444444444448"),
                columns: new[] { "Price", "PriceNote" },
                values: new object[] { 800m, "يبدأ من / سنوياً" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceNote",
                table: "Products");
        }
    }
}
