namespace Amora.Application.Messaging;

/// <summary>Queue: chat_voice_processed — .NET → Python worker.</summary>
public sealed class ChatVoiceProcessedMessage
{
    public string CorrelationId { get; init; } = string.Empty;

    public Guid MatchId { get; init; }

    public Guid UserId { get; init; }

    public string AudioUrl { get; init; } = string.Empty;

    public double DurationSeconds { get; init; }
}
