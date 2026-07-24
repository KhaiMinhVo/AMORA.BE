using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenReportModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReports_ReporterId_TargetUserId",
                table: "UserReports");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AiEvaluatedAt",
                table: "UserReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AiScore",
                table: "UserReports",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiVerdict",
                table: "UserReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "UserReports",
                keyColumn: "Id",
                keyValue: new Guid("41111111-1111-1111-1111-111111111111"),
                columns: new[] { "AiEvaluatedAt", "AiScore", "AiVerdict" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId_TargetUserId_CreatedAt",
                table: "UserReports",
                columns: new[] { "ReporterId", "TargetUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReports_ReporterId_TargetUserId_CreatedAt",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "AiEvaluatedAt",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "AiVerdict",
                table: "UserReports");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId_TargetUserId",
                table: "UserReports",
                columns: new[] { "ReporterId", "TargetUserId" },
                unique: true);
        }
    }
}
