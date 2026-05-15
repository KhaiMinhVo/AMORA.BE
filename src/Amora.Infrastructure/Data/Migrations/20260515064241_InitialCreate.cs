using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserBId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoicePosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PosterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MatchCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoicePosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceComments_VoicePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "VoicePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "DisplayName" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "alice.png", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Amora Alice" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "bob.png", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Amora Bob" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "carol.png", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Amora Carol" }
                });

            migrationBuilder.InsertData(
                table: "VoicePosts",
                columns: new[] { "Id", "AudioUrl", "CreatedAt", "MatchCount", "PosterId", "Status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "https://amora-s3.bucket.com/voices/post_1.m4a", new DateTimeOffset(new DateTime(2026, 5, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, new Guid("11111111-1111-1111-1111-111111111111"), "Open" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "https://amora-s3.bucket.com/voices/post_2.m4a", new DateTimeOffset(new DateTime(2026, 5, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, new Guid("22222222-2222-2222-2222-222222222222"), "Open" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchConnections_PostId",
                table: "MatchConnections",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchConnections_UserAId_Status",
                table: "MatchConnections",
                columns: new[] { "UserAId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchConnections_UserBId_Status",
                table: "MatchConnections",
                columns: new[] { "UserBId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceComments_PostId_CommenterId",
                table: "VoiceComments",
                columns: new[] { "PostId", "CommenterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceComments_PostId_Status",
                table: "VoiceComments",
                columns: new[] { "PostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VoicePosts_PosterId_Status",
                table: "VoicePosts",
                columns: new[] { "PosterId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchConnections");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VoiceComments");

            migrationBuilder.DropTable(
                name: "VoicePosts");
        }
    }
}
