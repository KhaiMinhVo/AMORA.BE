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
}
