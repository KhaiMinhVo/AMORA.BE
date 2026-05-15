namespace Amora.Application.Dtos.Messages;

public sealed class SendMessageRequest
{
    public string Type { get; set; } = "Voice";

    public string? ContentUrl { get; set; }

    public string? Content { get; set; }

    public int? Duration { get; set; }
}

public sealed class MessageItemDto
{
    public string MessageId { get; init; } = string.Empty;

    public Guid? SenderId { get; init; }

    public string Type { get; init; } = string.Empty;

    public string? ContentUrl { get; init; }

    public string? Content { get; init; }

    public int? Duration { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class MessageHistoryResponseDto
{
    public string? NextCursor { get; init; }

    public IReadOnlyList<MessageItemDto> Items { get; init; } = Array.Empty<MessageItemDto>();
}

public sealed class SendMessageResponseDto
{
    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = "Sent";

    public DateTimeOffset CreatedAt { get; init; }
}