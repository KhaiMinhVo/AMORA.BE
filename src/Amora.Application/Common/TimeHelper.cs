namespace Amora.Application.Common;

public static class TimeHelper
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(VietnamOffset).Date);
    }
}
