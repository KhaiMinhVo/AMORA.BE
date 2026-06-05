using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndBoosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoldUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsGold",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PremiumUntil",
                table: "Users",
                newName: "SubscriptionEndDate");

            migrationBuilder.AddColumn<int>(
                name: "MaxMatchSlots",
                table: "VoicePosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionType",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.CreateTable(
                name: "PostBoostRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoostType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostBoostRecords", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "VoicePosts",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "MaxMatchSlots",
                value: 3);

            migrationBuilder.UpdateData(
                table: "VoicePosts",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "MaxMatchSlots",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "IX_PostBoostRecords_PostId_ExpiresAt",
                table: "PostBoostRecords",
                columns: new[] { "PostId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PostBoostRecords_UserId_CreatedAt",
                table: "PostBoostRecords",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostBoostRecords");

            migrationBuilder.DropColumn(
                name: "MaxMatchSlots",
                table: "VoicePosts");

            migrationBuilder.DropColumn(
                name: "SubscriptionType",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SubscriptionEndDate",
                table: "Users",
                newName: "PremiumUntil");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GoldUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGold",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium" },
                values: new object[] { null, false, false });
        }
    }
}
