using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awai.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppAndMapLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "SiteInfo",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-1111-4111-8111-111111111111"),
                columns: new[] { "MapEmbedUrl", "MapUrl", "WhatsAppNumber" },
                values: new object[] { "https://maps.google.com/maps?q=V36X%2B84V&hl=ar&z=17&output=embed", "https://maps.app.goo.gl/9JnJBwVbKfeF15BS9", "218940001097" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "SiteInfo");

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-1111-4111-8111-111111111111"),
                columns: new[] { "MapEmbedUrl", "MapUrl" },
                values: new object[] { "https://www.google.com/maps?q=V36X+84V&hl=ar&z=17&output=embed", "https://www.google.com/maps?q=V36X+84V" });
        }
    }
}
