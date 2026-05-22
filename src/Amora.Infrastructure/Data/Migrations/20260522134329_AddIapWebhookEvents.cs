using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIapWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IapWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    RawPayload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IapWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IapWebhookEvents_Platform_EventId",
                table: "IapWebhookEvents",
                columns: new[] { "Platform", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IapWebhookEvents");
        }
    }
}
