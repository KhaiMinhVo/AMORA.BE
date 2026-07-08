using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRevivalFlaskTo100Hp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                column: "EffectJson",
                value: "{\"hp\":100, \"revive\":true}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                column: "EffectJson",
                value: "{\"hp\":50}");
        }
    }
}
