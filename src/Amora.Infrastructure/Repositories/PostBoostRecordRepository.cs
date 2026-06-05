using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class PostBoostRecordRepository : IPostBoostRecordRepository
{
    private readonly AmoraDbContext _dbContext;

    public PostBoostRecordRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PostBoostRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.PostBoostRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostBoostRecord>> GetActiveBoostsForPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PostBoostRecords
            .Where(x => x.PostId == postId && x.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
