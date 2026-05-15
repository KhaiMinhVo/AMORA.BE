using Amora.Domain.Entities;

namespace Amora.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task NotifyMatchCreatedAsync(MatchConnection matchConnection, CancellationToken cancellationToken = default);

    Task NotifyNewMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}