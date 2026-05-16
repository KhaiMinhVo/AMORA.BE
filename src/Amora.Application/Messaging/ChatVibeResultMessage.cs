namespace Amora.Application.Messaging;

/// <summary>Queue: chat_vibe_result — Python worker → .NET.</summary>
public sealed class ChatVibeResultMessage
{
    public string CorrelationId { get; init; } = string.Empty;

    public Guid MatchId { get; init; }

    public Guid UserId { get; init; }

    public int VibeScore { get; init; }

    public double EnergyRms { get; init; }

    public double PitchVariance { get; init; }

    public double SpeechRate { get; init; }

    public double Jitter { get; init; }

    public double DurationSeconds { get; init; }
}
