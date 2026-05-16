using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ProcessCoPresenceCommand(Guid MatchId) : IRequest<int>;
