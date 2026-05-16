using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ProcessTextMessagePetCommand(Guid MatchId, Guid SenderId) : IRequest;
