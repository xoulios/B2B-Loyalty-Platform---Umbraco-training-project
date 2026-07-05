using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PointsValue = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimedByMemberKey = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClaimedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCodes_Code",
                table: "OrderCodes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCodes");
        }
    }
}
