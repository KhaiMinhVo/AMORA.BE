namespace Amora.Domain.Enums;

public enum ItemType
{
    Consumable = 0,   // Vật phẩm tiêu hao chung
    Buff = 1,         // Vitamin Tăng Trưởng (EXP Booster)
    Clothes = 2,      // Trang phục (pet_clothes)
    Revival = 3,      // Thuốc Hồi Sinh
    Subscription = 4, // Gói Premium/Gold
    Toy = 5,          // Đồ chơi tương tác (Bóng, Cần câu mèo...)
    Special = 6,      // Đặc biệt (Thẻ đổi tên...)
    Food = 7,         // Thức ăn (pet_food)
    Water = 8         // Nước (pet_water)
}
