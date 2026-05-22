using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModerationUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BanReason", "BannedUntil", "IsBanned", "Role" },
                values: new object[] { null, null, false, "User" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "BanReason", "BannedUntil", "IsBanned", "Role" },
                values: new object[] { null, null, false, "User" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "BanReason", "BannedUntil", "IsBanned", "Role" },
                values: new object[] { null, null, false, "User" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AmoraGems", "AvatarUrl", "BanReason", "BannedUntil", "Bio", "City", "CreatedAt", "DateOfBirth", "DisplayName", "Email", "Gender", "Interests", "IsBanned", "IsProfileComplete", "LastCoPresenceCoinDate", "LastPetCoinRewardDate", "PasswordHash", "PetCoins", "Role" },
                values: new object[] { new Guid("99999999-9999-9999-9999-999999999999"), 0, "admin.png", null, null, null, null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Amora Admin", "admin@amora.app", "PreferNotToSay", null, false, false, null, null, null, 0, "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BannedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
