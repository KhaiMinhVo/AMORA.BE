using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IPostBoostRecordRepository
{
    Task AddAsync(PostBoostRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostBoostRecord>> GetActiveBoostsForPostAsync(Guid postId, CancellationToken cancellationToken = default);
}
