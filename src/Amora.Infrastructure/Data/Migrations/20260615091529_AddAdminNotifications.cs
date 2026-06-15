using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_CreatedAt",
                table: "AdminNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_IsRead",
                table: "AdminNotifications",
                column: "IsRead");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotifications");
        }
    }
}
