using Amora.Application.Dtos.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed record GetPetStatusQuery(Guid MatchId, Guid UserId) : IRequest<PetStatusDto>;
