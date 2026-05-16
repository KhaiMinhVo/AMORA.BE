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
}