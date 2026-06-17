using Amora.Domain.Enums;

namespace Amora.Application.Pets;

public static class PetFeatureUnlocks
{
    public static IReadOnlyList<string> ForStage(GrowthStage stage) => stage switch
    {
        GrowthStage.Sprout => new[] { "photo_once_per_day" },
        GrowthStage.Young => new[] { "photo_once_per_day", "voice_call_5min" },
        GrowthStage.Adult => new[] { "photo_once_per_day", "voice_call_5min", "video_call_3min", "photo_timeline" },
        GrowthStage.Legend => new[] { "photo_once_per_day", "voice_call_5min", "video_call_3min", "photo_timeline", "group_chat_4", "voice_archive" },
        _ => Array.Empty<string>()
    };

    public static string StageDisplayName(GrowthStage stage) => stage switch
    {
        GrowthStage.ResonanceSeed => "Hạt Cộng Hưởng",
        GrowthStage.Sprout => "Mầm Non",
        GrowthStage.Young => "Thú Nhỏ",
        GrowthStage.Adult => "Thú Trưởng Thành",
        GrowthStage.Legend => "Huyền Thoại",
        _ => "Unknown"
    };

    public static string PetTypeName(PetType type) => type switch
    {
        PetType.None => "Chưa nở",
        PetType.Dog => "Chó (Morning Birds)",
        PetType.Cat => "Mèo (Night Owls)",
        PetType.Rabbit => "Thỏ (Sprinters)",
        PetType.Otter => "Rái cá (Weekend Warriors)",
        _ => "Unknown"
    };

    public static string PetTypeDescription(PetType type) => type switch
    {
        PetType.None => "Mầm sống của tình cảm đang được ấp ủ.",
        PetType.Dog => "Dành cho nhau những lời chào đầu ngày chứng tỏ đối phương luôn ở vị trí số một trong tâm trí bạn mỗi khi thức giấc. Cún Con là biểu tượng của nguồn năng lượng và sự ưu tiên.",
        PetType.Cat => "Màn đêm là lúc những tâm hồn 'cú đêm' cởi mở nhất. Bé Mèo sẽ là người giữ lấy những lời tâm sự thầm kín và chân thật nhất của hai bạn.",
        PetType.Rabbit => "Năng động và chớp nhoáng. Dù bận rộn với guồng quay công việc, hai bạn vẫn biết cách tranh thủ thời gian gửi cho nhau những đoạn voice quan tâm đầy đáng yêu.",
        PetType.Otter => "Sự gắn kết bền chặt. Dù trong tuần có im ắng, cuối tuần vẫn là lúc hai bạn trò chuyện miệt mài, như loài Rái cá luôn nắm tay nhau để không bị lạc mất.",
        _ => "Unknown"
    };
}
