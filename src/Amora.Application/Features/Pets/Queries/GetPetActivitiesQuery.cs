using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed record GetPetActivitiesQuery(Guid MatchId, Guid UserId, int Page, int PageSize) : IRequest<PagedResult<PetActivityDto>>;
