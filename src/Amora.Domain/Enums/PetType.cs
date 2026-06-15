namespace Amora.Domain.Enums;

/// <summary>Hình dáng tiến hóa của Thú cưng dựa trên Đồng hồ sinh học (Chronotype) của cặp đôi.</summary>
public enum PetType
{
    None = 0,    // Trứng chưa nở
    Dog = 1,     // Nhắn tin buổi sáng (Morning Birds: 05:00 - 11:59)
    Cat = 2,     // Nhắn tin đêm khuya (Night Owls: 22:00 - 04:59)
    Rabbit = 3,  // Nhắn tin giờ hành chính (Sprinters: 12:00 - 21:59)
    Otter = 4    // Tương tác nhiều cuối tuần (Weekend Warriors)
}
