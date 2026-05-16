using Amora.Application.Dtos.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed record GetInventoryQuery(Guid UserId) : IRequest<IReadOnlyList<InventoryItemDto>>;
