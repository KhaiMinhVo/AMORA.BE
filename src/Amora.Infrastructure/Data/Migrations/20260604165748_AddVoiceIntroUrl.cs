using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceIntroUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoiceIntroUrl",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "VoiceIntroUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "VoiceIntroUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "VoiceIntroUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "VoiceIntroUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoiceIntroUrl",
                table: "Users");
        }
    }
}
