using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoRenewSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoRenewDurationDays",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AutoRenewPriceDiamonds",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoRenewEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AutoRenewDurationDays", "AutoRenewPriceDiamonds", "IsAutoRenewEnabled" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AutoRenewDurationDays", "AutoRenewPriceDiamonds", "IsAutoRenewEnabled" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AutoRenewDurationDays", "AutoRenewPriceDiamonds", "IsAutoRenewEnabled" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "AutoRenewDurationDays", "AutoRenewPriceDiamonds", "IsAutoRenewEnabled" },
                values: new object[] { 0, 0, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenewDurationDays",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AutoRenewPriceDiamonds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAutoRenewEnabled",
                table: "Users");
        }
    }
}
