using Amora.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Amora.Infrastructure.Data.Documents;

public sealed class ChatMessageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public Guid MatchId { get; set; }

    public Guid? SenderId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public MessageType MessageType { get; set; }

    public string? ContentUrl { get; set; }

    public string? Content { get; set; }

    public int? Duration { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}