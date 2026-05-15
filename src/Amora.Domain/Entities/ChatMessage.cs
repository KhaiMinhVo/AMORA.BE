using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class ChatMessage
{
    public string Id { get; set; } = string.Empty;

    public Guid MatchId { get; set; }

    public Guid? SenderId { get; set; }

    public MessageType MessageType { get; set; } = MessageType.Voice;

    public string? ContentUrl { get; set; }

    public string? Content { get; set; }

    public int? Duration { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}