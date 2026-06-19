using Amora.Application.Dtos.Admin;
using Amora.Application.Dtos.Support;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class SupportTicketService
{
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IUserRepository _userRepository;

    public SupportTicketService(
        ISupportTicketRepository supportTicketRepository,
        IUserRepository userRepository)
    {
        _supportTicketRepository = supportTicketRepository;
        _userRepository = userRepository;
    }

    public async Task CreateTicketAsync(Guid userId, CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.Type,
            ReferenceId = request.ReferenceId,
            Description = request.Description,
            Status = SupportTicketStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _supportTicketRepository.AddAsync(ticket, cancellationToken);
    }

    public async Task<PaginatedList<SupportTicketDto>> GetTicketsForAdminAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var tickets = await _supportTicketRepository.GetAllAsync(page, pageSize, cancellationToken);
        var totalCount = await _supportTicketRepository.GetTotalCountAsync(cancellationToken);

        var dtos = tickets.Select(t => new SupportTicketDto
        {
            Id = t.Id,
            UserId = t.UserId,
            UserDisplayName = t.User?.DisplayName ?? "Unknown",
            Type = t.Type,
            ReferenceId = t.ReferenceId,
            Description = t.Description,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ResolvedAt = t.ResolvedAt
        });

        return new PaginatedList<SupportTicketDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
