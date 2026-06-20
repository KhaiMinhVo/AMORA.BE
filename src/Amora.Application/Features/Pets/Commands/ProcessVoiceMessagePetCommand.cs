using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ProcessVoiceMessagePetCommand(Guid MatchId, Guid SenderId, double DurationSeconds) : IRequest;
