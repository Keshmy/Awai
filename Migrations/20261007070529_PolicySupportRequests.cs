using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awai.Migrations
{
    /// <inheritdoc />
    public partial class PolicySupportRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicySupportRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsHandled = table.Column<bool>(type: "bit", nullable: false),
                    MatchedIssuedPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicySupportRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicySupportRequests_IsHandled_Created",
                table: "PolicySupportRequests",
                columns: new[] { "IsHandled", "Created" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicySupportRequests");
        }
    }
}
