namespace Amora.Application.Dtos.Messages;

public sealed class MarkMessagesAsReadRequest
{
    public string MessageId { get; set; } = string.Empty;
}
