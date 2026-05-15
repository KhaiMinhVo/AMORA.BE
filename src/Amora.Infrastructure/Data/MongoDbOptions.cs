namespace Amora.Infrastructure.Data;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string DatabaseName { get; set; } = "AmoraChatDb";

    public string MessagesCollectionName { get; set; } = "Messages";
}