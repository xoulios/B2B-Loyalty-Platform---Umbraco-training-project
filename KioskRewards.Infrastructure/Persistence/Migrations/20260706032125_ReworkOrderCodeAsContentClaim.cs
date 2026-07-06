using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KioskRewards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkOrderCodeAsContentClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderCodes_Code",
                table: "OrderCodes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "OrderCodes");

            migrationBuilder.DropColumn(
                name: "PointsValue",
                table: "OrderCodes");

            migrationBuilder.DropColumn(
                name: "ProductDescription",
                table: "OrderCodes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClaimedUtc",
                table: "OrderCodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClaimedByMemberKey",
                table: "OrderCodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContentKey",
                table: "OrderCodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OrderCodes_ContentKey",
                table: "OrderCodes",
                column: "ContentKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderCodes_ContentKey",
                table: "OrderCodes");

            migrationBuilder.DropColumn(
                name: "ContentKey",
                table: "OrderCodes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClaimedUtc",
                table: "OrderCodes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClaimedByMemberKey",
                table: "OrderCodes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OrderCodes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PointsValue",
                table: "OrderCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductDescription",
                table: "OrderCodes",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCodes_Code",
                table: "OrderCodes",
                column: "Code",
                unique: true);
        }
    }
}
