using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationStatsMockData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "UserReports",
                columns: new[] { "Id", "CreatedAt", "Description", "Reason", "ReporterId", "Status", "TargetCommentId", "TargetPostId", "TargetUserId" },
                values: new object[] { new Guid("41111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Quấy rối người khác", "Harassment", new Guid("11111111-1111-1111-1111-111111111111"), "Pending", null, null, new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AutoRenewDurationDays", "AutoRenewPriceDiamonds", "AvatarUrl", "Bio", "City", "CreatedAt", "DateOfBirth", "Diamonds", "DisplayName", "Email", "ExtraMatchSlots", "Gender", "GoogleId", "Interests", "IsAutoRenewEnabled", "IsBanned", "IsProfileComplete", "LastActiveAt", "LastCoPresenceCoinDate", "LastDailyBonus", "LastDiamondRewardDate", "PasswordHash", "PhoneNumber", "Photos", "PreferredVoiceTones", "ProfileBonusClaimed", "RequiresPasswordUpdate", "Role", "SubscriptionEndDate", "TargetGender", "TrustScore", "VoiceIntroDuration", "VoiceIntroUrl", "VoicePrivacy" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), 0, 0, "vipham.png", null, null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, "Nguyễn Văn Vi Phạm", "vipham@gmail.com", 0, "PreferNotToSay", null, null, false, false, false, null, null, null, null, null, null, new string[0], new int[0], false, false, "User", null, 0, 80, null, null, 0 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 0, 0, "khangcao.png", null, null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, "Trần Kháng Cáo", "khangcao@gmail.com", 0, "PreferNotToSay", null, null, false, true, false, null, null, null, null, null, null, new string[0], new int[0], false, false, "User", null, 0, 80, null, null, 0 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 0, 0, "aiban.png", null, null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, "Người Bị AI Cấm", "aiban@gmail.com", 0, "PreferNotToSay", null, null, false, true, false, null, null, null, null, null, null, new string[0], new int[0], false, false, "User", null, 0, 80, null, null, 0 }
                });

            migrationBuilder.InsertData(
                table: "UserBans",
                columns: new[] { "Id", "AppealReason", "AppealStatus", "BanReason", "BannedUntil", "CreatedAt", "IsActive", "UserId" },
                values: new object[,]
                {
                    { new Guid("77777777-7777-7777-7777-777777777777"), "Đây là hiểu nhầm", 0, "Banned by Admin", null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("55555555-5555-5555-5555-555555555555") },
                    { new Guid("88888888-8888-8888-8888-888888888888"), null, null, "[AI AUTOMATED] Toxic behavior detected", null, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, new Guid("66666666-6666-6666-6666-666666666666") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserBans",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "UserBans",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "UserReports",
                keyColumn: "Id",
                keyValue: new Guid("41111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));
        }
    }
}
