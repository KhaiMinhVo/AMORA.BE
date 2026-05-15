using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Amora.Infrastructure.Data.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Amora.Infrastructure.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly IMongoCollection<ChatMessageDocument> _collection;

    public ChatMessageRepository(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var database = mongoClient.GetDatabase(options.Value.DatabaseName);
        _collection = database.GetCollection<ChatMessageDocument>(options.Value.MessagesCollectionName);
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        var document = new ChatMessageDocument
        {
            Id = message.Id,
            MatchId = message.MatchId,
            SenderId = message.SenderId,
            MessageType = message.MessageType,
            ContentUrl = message.ContentUrl,
            Content = message.Content,
            Duration = message.Duration,
            CreatedAt = message.CreatedAt
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<(IReadOnlyList<ChatMessage> Items, string? NextCursor)> GetByMatchAsync(
        Guid matchId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ChatMessageDocument>.Filter.Eq(x => x.MatchId, matchId);

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var cursorDocument = await _collection
                .Find(Builders<ChatMessageDocument>.Filter.And(
                    Builders<ChatMessageDocument>.Filter.Eq(x => x.MatchId, matchId),
                    Builders<ChatMessageDocument>.Filter.Eq(x => x.Id, cursor)))
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorDocument is not null)
            {
                filter &= Builders<ChatMessageDocument>.Filter.Or(
                    Builders<ChatMessageDocument>.Filter.Lt(x => x.CreatedAt, cursorDocument.CreatedAt),
                    Builders<ChatMessageDocument>.Filter.And(
                        Builders<ChatMessageDocument>.Filter.Eq(x => x.CreatedAt, cursorDocument.CreatedAt),
                        Builders<ChatMessageDocument>.Filter.Lt(x => x.Id, cursorDocument.Id)));
            }
        }

        var documents = await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Limit(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = documents.Count > limit;
        var page = documents.Take(limit).Select(MapToDomain).ToList();
        var nextCursor = hasMore ? page.LastOrDefault()?.Id : null;

        return (page, nextCursor);
    }

    private static ChatMessage MapToDomain(ChatMessageDocument document)
    {
        return new ChatMessage
        {
            Id = document.Id,
            MatchId = document.MatchId,
            SenderId = document.SenderId,
            MessageType = document.MessageType,
            ContentUrl = document.ContentUrl,
            Content = document.Content,
            Duration = document.Duration,
            CreatedAt = document.CreatedAt
        };
    }
}