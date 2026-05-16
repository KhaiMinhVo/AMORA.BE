using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Amora.Infrastructure.Data.Documents;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Repositories;

public sealed class ChatReadStateRepository : IChatReadStateRepository
{
    private readonly AmoraDbContext _db;
    private readonly IMongoCollection<ChatMessageDocument> _messages;

    public ChatReadStateRepository(AmoraDbContext db, IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        _db = db;
        var database = mongoClient.GetDatabase(options.Value.DatabaseName);
        _messages = database.GetCollection<ChatMessageDocument>(options.Value.MessagesCollectionName);
    }

    public Task<ChatReadState?> GetAsync(Guid userId, Guid matchId, CancellationToken cancellationToken = default)
        => _db.ChatReadStates.FirstOrDefaultAsync(x => x.UserId == userId && x.MatchId == matchId, cancellationToken);

    public async Task UpsertReadAsync(Guid userId, Guid matchId, DateTimeOffset readAt, CancellationToken cancellationToken = default)
    {
        var state = await GetAsync(userId, matchId, cancellationToken);
        if (state is null)
        {
            await _db.ChatReadStates.AddAsync(new ChatReadState
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MatchId = matchId,
                LastReadAt = readAt,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        else
        {
            state.LastReadAt = readAt;
            state.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(Guid userId, Guid matchId, DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ChatMessageDocument>.Filter.And(
            Builders<ChatMessageDocument>.Filter.Eq(x => x.MatchId, matchId),
            Builders<ChatMessageDocument>.Filter.Gt(x => x.CreatedAt, since),
            Builders<ChatMessageDocument>.Filter.Ne(x => x.SenderId, userId),
            Builders<ChatMessageDocument>.Filter.Ne(x => x.MessageType, Domain.Enums.MessageType.System));

        return (int)await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountUnreadByMatchesAsync(
        Guid userId,
        IReadOnlyList<Guid> matchIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, int>();
        if (matchIds.Count == 0) return result;

        var states = await _db.ChatReadStates
            .Where(x => x.UserId == userId && matchIds.Contains(x.MatchId))
            .ToListAsync(cancellationToken);

        foreach (var matchId in matchIds)
        {
            var since = states.FirstOrDefault(s => s.MatchId == matchId)?.LastReadAt
                        ?? DateTimeOffset.MinValue;
            result[matchId] = await CountUnreadAsync(userId, matchId, since, cancellationToken);
        }

        return result;
    }
}
