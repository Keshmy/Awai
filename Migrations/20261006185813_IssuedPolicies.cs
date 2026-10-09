using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awai.Migrations
{
    /// <inheritdoc />
    public partial class IssuedPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuedPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AccessToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProductKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InsuredName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TotalPremium = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedPolicies_AccessToken",
                table: "IssuedPolicies",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssuedPolicies_PolicyNumber",
                table: "IssuedPolicies",
                column: "PolicyNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuedPolicies");
        }
    }
}
