using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amora.Domain.Entities;
using Amora.Domain.Enums;

namespace Amora.Domain.Interfaces;

public interface IUserBanRepository
{
    Task<UserBan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserBan?> GetActiveBanByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<UserBan> Items, int TotalCount)> GetPendingAppealsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(UserBan userBan, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserBan userBan, CancellationToken cancellationToken = default);
}
