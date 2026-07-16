using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record SetInitialPetNameCommand(Guid UserId, Guid MatchId, string Name) : IRequest<SetInitialPetNameResult>;

public sealed record SetInitialPetNameResult(Guid MatchId, string Name);
