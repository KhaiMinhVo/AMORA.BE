using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "LastDailyBonus",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileBonusClaimed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrustScore",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "LastDailyBonus", "ProfileBonusClaimed", "TrustScore" },
                values: new object[] { null, false, 100 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "LastDailyBonus", "ProfileBonusClaimed", "TrustScore" },
                values: new object[] { null, false, 100 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "LastDailyBonus", "ProfileBonusClaimed", "TrustScore" },
                values: new object[] { null, false, 100 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "LastDailyBonus", "ProfileBonusClaimed", "TrustScore" },
                values: new object[] { null, false, 100 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDailyBonus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileBonusClaimed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrustScore",
                table: "Users");
        }
    }
}
