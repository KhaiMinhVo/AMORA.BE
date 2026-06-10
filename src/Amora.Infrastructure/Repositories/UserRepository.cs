using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AmoraDbContext _dbContext;

    public UserRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<AppUser?> GetByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<AppUser?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<AppUser?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.GoogleId == googleId, cancellationToken);

    public Task<AppUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetAllUsersAsync(int page, int pageSize, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => 
                x.Email != null && x.Email.Contains(keyword) || 
                x.DisplayName.Contains(keyword));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}