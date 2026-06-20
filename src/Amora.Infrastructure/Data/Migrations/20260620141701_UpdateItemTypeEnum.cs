using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"ItemType\" = 'Food' WHERE \"ItemType\" = 'Consumable' AND \"Code\" = 'pet_food'");
            migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"ItemType\" = 'Water' WHERE \"ItemType\" = 'Consumable' AND \"Code\" = 'water'");
            migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"ItemType\" = 'Clothes' WHERE \"ItemType\" = 'Cosmetic'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"ItemType\" = 'Cosmetic' WHERE \"ItemType\" = 'Clothes'");
            migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"ItemType\" = 'Consumable' WHERE \"ItemType\" IN ('Food', 'Water')");
        }
    }
}
