using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHandshake24h : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "MatchConnections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Backfill: mọi match hiện có nhận 24h kể từ CreatedAt (tránh expire ngay do default 0001-01-01)
            migrationBuilder.Sql(
                """
                UPDATE "MatchConnections"
                SET "ExpiresAt" = "CreatedAt" + INTERVAL '24 hours';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MatchConnections_Status_ExpiresAt",
                table: "MatchConnections",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchConnections_Status_ExpiresAt",
                table: "MatchConnections");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "MatchConnections");
        }
    }
}
