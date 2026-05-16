using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPetSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmoraGems",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PetCoins",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    Mood = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rp = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    LastInteractionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsecutiveNegativeVibes = table.Column<int>(type: "integer", nullable: false),
                    LastPartnerMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HpGainedIn24h = table.Column<int>(type: "integer", nullable: false),
                    HpGainWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RpStatsDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RpFromTextToday = table.Column<int>(type: "integer", nullable: false),
                    RpFromVoiceToday = table.Column<int>(type: "integer", nullable: false),
                    OnlineBonusGrantedToday = table.Column<bool>(type: "boolean", nullable: false),
                    ConsecutiveHighHpDays = table.Column<int>(type: "integer", nullable: false),
                    LastHpSnapshotDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HpSnapshotSum = table.Column<double>(type: "double precision", nullable: false),
                    HpSnapshotCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveBuffsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_MatchConnections_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PricePetCoins = table.Column<int>(type: "integer", nullable: false),
                    PriceAmoraGems = table.Column<int>(type: "integer", nullable: false),
                    EffectJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetStateHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetStateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetStateHistories_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PetCoinsDelta = table.Column<int>(type: "integer", nullable: false),
                    AmoraGemsDelta = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PetTransactions_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PetTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInventories_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserInventories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f1000001-0001-4001-8001-000000000001"), "energy_cookie", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bánh Quy Năng Lượng", "{\"hp\":30}", true, "Consumable", "Bánh Quy Năng Lượng", 0, 50, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000002"), "gentle_bath", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sữa Tắm Dịu Nhẹ", "{\"buff\":\"AffectionateMood\",\"hours\":2}", true, "Buff", "Sữa Tắm Dịu Nhẹ", 5, 80, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000003"), "growth_potion", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lọ Thuốc Tăng Trưởng", "{\"buff\":\"DoubleVoiceRp\",\"hours\":6}", true, "Buff", "Lọ Thuốc Tăng Trưởng", 10, 120, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000004"), "resonance_candy", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kẹo Cộng Hưởng", "{\"rp\":10}", true, "Consumable", "Kẹo Cộng Hưởng", 0, 40, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000005"), "revival_flask", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bình Hồi Sinh", "{\"hp\":50}", true, "Revival", "Bình Hồi Sinh", 20, 200, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000006"), "fire_fox_skin", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Da Cáo Lửa", "{}", true, "Cosmetic", "Da Cáo Lửa", 15, 150, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000007"), "memory_collar", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vòng Cổ Kỷ Niệm", "{}", true, "Cosmetic", "Vòng Cổ Kỷ Niệm", 30, 300, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AmoraGems", "PetCoins" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "AmoraGems", "PetCoins" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "AmoraGems", "PetCoins" },
                values: new object[] { 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_IsFrozen_LastInteractionAt",
                table: "Pets",
                columns: new[] { "IsFrozen", "LastInteractionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_MatchId",
                table: "Pets",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PetStateHistories_PetId_CreatedAt",
                table: "PetStateHistories",
                columns: new[] { "PetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PetTransactions_ShopItemId",
                table: "PetTransactions",
                column: "ShopItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PetTransactions_UserId_CreatedAt",
                table: "PetTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_Code",
                table: "ShopItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_ShopItemId",
                table: "UserInventories",
                column: "ShopItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_UserId_ShopItemId",
                table: "UserInventories",
                columns: new[] { "UserId", "ShopItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetStateHistories");

            migrationBuilder.DropTable(
                name: "PetTransactions");

            migrationBuilder.DropTable(
                name: "UserInventories");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropColumn(
                name: "AmoraGems",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PetCoins",
                table: "Users");
        }
    }
}
