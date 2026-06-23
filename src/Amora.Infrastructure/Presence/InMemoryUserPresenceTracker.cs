using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Amora.Application.Abstractions;
using Amora.Domain.Interfaces;

namespace Amora.Infrastructure.Presence;

public sealed class InMemoryUserPresenceTracker : IUserPresenceTracker
{
    // Dictionary mapping UserId -> HashSet of ConnectionIds
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, bool>> _userConnections = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public InMemoryUserPresenceTracker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task UserConnectedAsync(Guid userId, string connectionId)
    {
        var connections = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, bool>());
        
        // If this is the very first connection for this user
        bool isFirstConnection = connections.IsEmpty;
        
        connections.TryAdd(connectionId, true);

        if (isFirstConnection)
        {
            // The user just came online
            using var scope = _scopeFactory.CreateScope();
            
            // Clear LastActiveAt in database
            var dbContext = scope.ServiceProvider.GetRequiredService<Amora.Infrastructure.Data.AmoraDbContext>();
            var user = await dbContext.Users.FindAsync(new object[] { userId });
            if (user != null)
            {
                user.LastActiveAt = null;
                await dbContext.SaveChangesAsync();
            }

            var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
            await realtimeNotifier.NotifyUserPresenceChangedAsync(userId, isOnline: true, lastActiveAt: null);

        }
    }

    public async Task UserDisconnectedAsync(Guid userId, string connectionId)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);

            if (connections.IsEmpty)
            {
                // The user has no more active connections
                _userConnections.TryRemove(userId, out _);
                
                using var scope = _scopeFactory.CreateScope();
                
                // Update LastActiveAt in database
                var dbContext = scope.ServiceProvider.GetRequiredService<Amora.Infrastructure.Data.AmoraDbContext>();
                var user = await dbContext.Users.FindAsync(new object[] { userId });
                DateTimeOffset? lastActiveAt = null;
                if (user != null)
                {
                    user.LastActiveAt = DateTimeOffset.UtcNow;
                    lastActiveAt = user.LastActiveAt;
                    await dbContext.SaveChangesAsync();
                }

                var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
                await realtimeNotifier.NotifyUserPresenceChangedAsync(userId, isOnline: false, lastActiveAt: lastActiveAt);
            }
        }
    }

    public Task<bool> IsOnlineAsync(Guid userId)
    {
        return Task.FromResult(_userConnections.ContainsKey(userId));
    }

    public Task<IReadOnlyDictionary<Guid, bool>> GetOnlineUsersAsync(IReadOnlyList<Guid> userIds)
    {
        var result = new Dictionary<Guid, bool>();
        foreach (var userId in userIds)
        {
            result[userId] = _userConnections.ContainsKey(userId);
        }
        return Task.FromResult<IReadOnlyDictionary<Guid, bool>>(result);
    }
}
