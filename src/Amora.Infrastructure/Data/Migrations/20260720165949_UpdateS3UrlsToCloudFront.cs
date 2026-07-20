using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateS3UrlsToCloudFront : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPushTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPushTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPushTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_Token",
                table: "UserPushTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_UserId_DeviceId",
                table: "UserPushTokens",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            var oldUrl = "https://amora-voice-bucket.s3.ap-southeast-1.amazonaws.com";
            var newUrl = "https://cdn.amora.pro.vn";

            migrationBuilder.Sql($@"
                UPDATE ""Users"" SET ""AvatarUrl"" = REPLACE(""AvatarUrl"", '{oldUrl}', '{newUrl}') WHERE ""AvatarUrl"" LIKE '{oldUrl}%';
                UPDATE ""Users"" SET ""VoiceIntroUrl"" = REPLACE(""VoiceIntroUrl"", '{oldUrl}', '{newUrl}') WHERE ""VoiceIntroUrl"" LIKE '{oldUrl}%';
                UPDATE ""ShopItems"" SET ""ImageUrl"" = REPLACE(""ImageUrl"", '{oldUrl}', '{newUrl}') WHERE ""ImageUrl"" LIKE '{oldUrl}%';
                UPDATE ""VoicePosts"" SET ""AudioUrl"" = REPLACE(""AudioUrl"", '{oldUrl}', '{newUrl}') WHERE ""AudioUrl"" LIKE '{oldUrl}%';
                UPDATE ""VoiceComments"" SET ""AudioUrl"" = REPLACE(""AudioUrl"", '{oldUrl}', '{newUrl}') WHERE ""AudioUrl"" LIKE '{oldUrl}%';
                UPDATE ""PetVibeData"" SET ""CleanAudioUrl"" = REPLACE(""CleanAudioUrl"", '{oldUrl}', '{newUrl}') WHERE ""CleanAudioUrl"" LIKE '{oldUrl}%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPushTokens");
        }
    }
}
