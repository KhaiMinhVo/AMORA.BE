using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly AmoraDbContext _context;

    public SupportTicketRepository(AmoraDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Using EF Core tracking
        return await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<SupportTicket>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets.CountAsync(cancellationToken);
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
