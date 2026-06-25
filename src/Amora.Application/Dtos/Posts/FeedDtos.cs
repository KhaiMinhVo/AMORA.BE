namespace Amora.Application.Dtos.Posts;

public sealed class PosterPreviewDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class FeedPostItemDto
{
    public Guid Id { get; init; }

    public PosterPreviewDto Poster { get; init; } = new();

    public string AudioUrl { get; init; } = string.Empty;

    public int MatchCount { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public bool IsBoosted { get; init; }

    public int MaxMatchSlots { get; init; }
}

public sealed class FeedResponseDto
{
    public int TotalCount { get; init; }

    public IReadOnlyList<FeedPostItemDto> Items { get; init; } = Array.Empty<FeedPostItemDto>();
}