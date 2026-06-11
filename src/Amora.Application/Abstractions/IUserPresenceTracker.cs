using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amora.Application.Abstractions;

public interface IUserPresenceTracker
{
    Task UserConnectedAsync(Guid userId, string connectionId);
    Task UserDisconnectedAsync(Guid userId, string connectionId);
    Task<bool> IsOnlineAsync(Guid userId);
    Task<IReadOnlyDictionary<Guid, bool>> GetOnlineUsersAsync(IReadOnlyList<Guid> userIds);
}
