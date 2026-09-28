using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awai.Migrations
{
    /// <inheritdoc />
    public partial class BrandLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-1111-4111-8111-111111111111"),
                column: "LogoUrl",
                value: "logo-mark.png");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-1111-4111-8111-111111111111"),
                column: "LogoUrl",
                value: "logo.svg");
        }
    }
}
