using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record UseItemCommand(Guid UserId, Guid MatchId, Guid ItemId) : IRequest;
