using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePetMood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveNegativeVibes",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "Mood",
                table: "Pets");

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                columns: new[] { "EffectJson", "ItemType" },
                values: new object[] { "{\"hp\":20}", "Consumable" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveNegativeVibes",
                table: "Pets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Mood",
                table: "Pets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                columns: new[] { "EffectJson", "ItemType" },
                values: new object[] { "{\"buff\":\"AffectionateMood\",\"hours\":2}", "Buff" });
        }
    }
}
