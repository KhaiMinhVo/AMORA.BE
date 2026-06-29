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
            ItemName = t.ShopItem?.Name ?? GetDefaultItemName(t.TransactionType)
        }).ToList();
    }

    private static string GetDefaultItemName(string transactionType)
    {
        return transactionType switch
        {
            "IapGemPurchase" => "Nạp Kim Cương (App Store/Google Play)",
            "PayOsPurchase" => "Nạp Kim Cương (Chuyển khoản)",
            "IapRefund" => "Hoàn Tiền Kim Cương",
            "Ad Reward" => "Xem Quảng Cáo",
            _ => transactionType
        };
    }
}
