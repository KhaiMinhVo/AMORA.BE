using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedCurrencyAndVnPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000006"));

            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000007"));

            migrationBuilder.DropColumn(
                name: "AmoraGems",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PriceAmoraGems",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "AmoraGemsDelta",
                table: "PetTransactions");

            migrationBuilder.RenameColumn(
                name: "PetCoins",
                table: "Users",
                newName: "Diamonds");

            migrationBuilder.RenameColumn(
                name: "LastPetCoinRewardDate",
                table: "Users",
                newName: "LastDiamondRewardDate");

            migrationBuilder.RenameColumn(
                name: "PricePetCoins",
                table: "ShopItems",
                newName: "PriceDiamonds");

            migrationBuilder.RenameColumn(
                name: "PetCoinsDelta",
                table: "PetTransactions",
                newName: "DiamondsDelta");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GoldUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGold",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PremiumUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountVnd = table.Column<int>(type: "integer", nullable: false),
                    DiamondsReceived = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000001"),
                columns: new[] { "Code", "Description", "Name", "PriceDiamonds" },
                values: new object[] { "pet_food", "Túi Thức Ăn Cho Pet", "Túi Thức Ăn Cho Pet", 15 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                column: "PriceDiamonds",
                value: 20);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000003"),
                column: "PriceDiamonds",
                value: 30);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000004"),
                column: "PriceDiamonds",
                value: 10);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                column: "PriceDiamonds",
                value: 50);

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceDiamonds", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f1000001-0001-4001-8001-000000000010"), "premium_7d", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Premium 7 Days", "{\"premium_days\":7}", true, "Subscription", "Premium 7 Days", 70, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000011"), "premium_30d", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Premium 1 Month", "{\"premium_days\":30}", true, "Subscription", "Premium 1 Month", 138, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000012"), "gold_7d", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gold 7 Days", "{\"gold_days\":7}", true, "Subscription", "Gold 7 Days", 98, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000013"), "gold_30d", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gold 1 Month", "{\"gold_days\":30}", true, "Subscription", "Gold 1 Month", 198, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium", "PremiumUntil" },
                values: new object[] { null, false, false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium", "PremiumUntil" },
                values: new object[] { null, false, false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium", "PremiumUntil" },
                values: new object[] { null, false, false, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "GoldUntil", "IsGold", "IsPremium", "PremiumUntil" },
                values: new object[] { null, false, false, null });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId_CreatedAt",
                table: "PaymentTransactions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000010"));

            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000011"));

            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000012"));

            migrationBuilder.DeleteData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000013"));

            migrationBuilder.DropColumn(
                name: "GoldUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsGold",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumUntil",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LastDiamondRewardDate",
                table: "Users",
                newName: "LastPetCoinRewardDate");

            migrationBuilder.RenameColumn(
                name: "Diamonds",
                table: "Users",
                newName: "PetCoins");

            migrationBuilder.RenameColumn(
                name: "PriceDiamonds",
                table: "ShopItems",
                newName: "PricePetCoins");

            migrationBuilder.RenameColumn(
                name: "DiamondsDelta",
                table: "PetTransactions",
                newName: "PetCoinsDelta");

            migrationBuilder.AddColumn<int>(
                name: "AmoraGems",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceAmoraGems",
                table: "ShopItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AmoraGemsDelta",
                table: "PetTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000001"),
                columns: new[] { "Code", "Description", "Name", "PriceAmoraGems", "PricePetCoins" },
                values: new object[] { "energy_cookie", "Bánh Quy Năng Lượng", "Bánh Quy Năng Lượng", 0, 50 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000002"),
                columns: new[] { "PriceAmoraGems", "PricePetCoins" },
                values: new object[] { 5, 80 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000003"),
                columns: new[] { "PriceAmoraGems", "PricePetCoins" },
                values: new object[] { 10, 120 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000004"),
                columns: new[] { "PriceAmoraGems", "PricePetCoins" },
                values: new object[] { 0, 40 });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("f1000001-0001-4001-8001-000000000005"),
                columns: new[] { "PriceAmoraGems", "PricePetCoins" },
                values: new object[] { 20, 200 });

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f1000001-0001-4001-8001-000000000006"), "fire_fox_skin", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Da Cáo Lửa", "{}", true, "Cosmetic", "Da Cáo Lửa", 15, 150, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f1000001-0001-4001-8001-000000000007"), "memory_collar", new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vòng Cổ Kỷ Niệm", "{}", true, "Cosmetic", "Vòng Cổ Kỷ Niệm", 30, 300, new DateTimeOffset(new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "AmoraGems",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "AmoraGems",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "AmoraGems",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "AmoraGems",
                value: 0);
        }
    }
}
