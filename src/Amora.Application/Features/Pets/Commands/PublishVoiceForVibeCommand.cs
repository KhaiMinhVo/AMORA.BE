using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record PublishVoiceForVibeCommand(
    Guid MatchId,
    Guid UserId,
    string AudioUrl,
    double DurationSeconds) : IRequest;
