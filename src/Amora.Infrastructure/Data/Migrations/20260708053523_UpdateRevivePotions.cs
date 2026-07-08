using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRevivePotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                columns: new[] { "Code", "Description", "EffectJson", "Name" },
                values: new object[] { "revive_potion_50", "Thuốc Hồi Sinh (Revive Potion)", "{\"hp\":50, \"revive\":true}", "Thuốc Hồi Sinh (Revive Potion)" });

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "Code", "CreatedAt", "DailyPurchaseLimit", "Description", "EffectJson", "ImageUrl", "IsActive", "ItemType", "MinStage", "Name", "PriceDiamonds", "UpdatedAt" },
                values: new object[] { new Guid("f1000001-0001-4001-8001-000000000008"), "revive_potion_30", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, "Thuốc Hồi Sinh (Recovery Potion)", "{\"hp\":30, \"revive\":true}", null, true, "Revival", null, "Thuốc Hồi Sinh (Recovery Potion)", 30, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000008"));

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                columns: new[] { "Code", "Description", "EffectJson", "Name" },
                values: new object[] { "revival_flask", "Bình Hồi Sinh", "{\"hp\":100, \"revive\":true}", "Bình Hồi Sinh" });
        }
    }
}
