using Amora.Application.Dtos.Pets;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    private readonly IPetTransactionRepository _transactionRepository;

    public GetTransactionsQueryHandler(IPetTransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<IReadOnlyList<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _transactionRepository.GetByUserAsync(request.UserId, request.Limit, cancellationToken);
        return rows.Select(t => new TransactionDto
        {
            Id = t.Id,
            TransactionType = t.TransactionType,
            DiamondsDelta = t.DiamondsDelta,
            CreatedAt = t.CreatedAt,
            ItemName = t.ShopItem?.Name
        }).ToList();
    }
}
