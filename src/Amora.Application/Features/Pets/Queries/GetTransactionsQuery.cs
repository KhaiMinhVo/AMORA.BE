using Amora.Application.Dtos.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed record GetTransactionsQuery(Guid UserId, int Limit = 50) : IRequest<IReadOnlyList<TransactionDto>>;
