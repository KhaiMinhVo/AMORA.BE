namespace Amora.Application.Dtos.Comments;

public sealed class CommenterPreviewDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class VoiceCommentItemDto
{
    public Guid CommentId { get; init; }

    public CommenterPreviewDto Commenter { get; init; } = new();

    public string AudioUrl { get; init; } = string.Empty;

    public int Duration { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class VoiceCommentListResponseDto
{
    public int TotalCount { get; init; }

    public IReadOnlyList<VoiceCommentItemDto> Items { get; init; } = Array.Empty<VoiceCommentItemDto>();
}

public sealed class CreateCommentResponseDto
{
    public Guid CommentId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}