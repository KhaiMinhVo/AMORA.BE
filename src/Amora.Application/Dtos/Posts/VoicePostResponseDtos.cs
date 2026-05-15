namespace Amora.Application.Dtos.Posts;

public sealed class CreateVoicePostResponseDto
{
    public Guid PostId { get; init; }

    public Guid PosterId { get; init; }

    public string AudioUrl { get; init; } = string.Empty;

    public int MatchCount { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}