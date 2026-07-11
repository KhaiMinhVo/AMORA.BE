using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHpAndExpRewardToShopItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpReward",
                table: "ShopItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HpReward",
                table: "ShopItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000001"),
                columns: new[] { "ExpReward", "HpReward", "ItemType" },
                values: new object[] { 10, 30, "Food" });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 20 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000003"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000004"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 10, 0 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 50 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000008"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 30 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000010"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000011"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000012"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000013"),
                columns: new[] { "ExpReward", "HpReward" },
                values: new object[] { 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpReward",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "HpReward",
                table: "ShopItems");

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000001"),
                column: "ItemType",
                value: "Consumable");
        }
    }
}
