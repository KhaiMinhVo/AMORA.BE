namespace Amora.Application.Dtos.Matches;

public sealed class CreateMatchRequest
{
    public Guid PostId { get; set; }

    public Guid CommentId { get; set; }
}

public sealed class MatchCreatedResponseDto
{
    public Guid MatchId { get; init; }

    public Guid UserB_Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool PostClosed { get; init; }

    /// <summary>Handshake 24h: hạn chót phải có tin nhắn (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed class PartnerPreviewDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class LastMessagePreviewDto
{
    public string Type { get; init; } = string.Empty;

    public string? ContentUrl { get; init; }

    public string? Content { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PetStateDto
{
    public int Hp { get; init; }

    public int Level { get; init; }
}

public sealed class InboxItemDto
{
    public Guid MatchId { get; init; }

    public PartnerPreviewDto Partner { get; init; } = new();

    public LastMessagePreviewDto? LastMessage { get; init; }

    public int UnreadCount { get; init; }

    public PetStateDto PetState { get; init; } = new();

    /// <summary>Handshake 24h: thời điểm match sẽ hết hạn nếu không nhắn tin.</summary>
    public DateTimeOffset ExpiresAt { get; init; }
}