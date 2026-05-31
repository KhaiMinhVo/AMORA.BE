using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPetGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyPurchaseLimit",
                table: "ShopItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ShopItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinStage",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeathTime",
                table: "Pets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquippedCosmeticsJson",
                table: "Pets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDead",
                table: "Pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastWaterClaimAt",
                table: "Pets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Pets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WaterClaimDate",
                table: "Pets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "WaterClaimsToday",
                table: "Pets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000001"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000003"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000004"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000010"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000011"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000012"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000013"),
                columns: new[] { "DailyPurchaseLimit", "ImageUrl", "MinStage" },
                values: new object[] { 0, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyPurchaseLimit",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "MinStage",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "DeathTime",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "EquippedCosmeticsJson",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "IsDead",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "LastWaterClaimAt",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "WaterClaimDate",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "WaterClaimsToday",
                table: "Pets");
        }
    }
}
