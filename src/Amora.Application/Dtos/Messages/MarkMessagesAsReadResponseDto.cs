namespace Amora.Application.Dtos.Messages;

public sealed class MarkMessagesAsReadResponseDto
{
    public Guid MatchId { get; set; }
    public string LastReadMessageId { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}
