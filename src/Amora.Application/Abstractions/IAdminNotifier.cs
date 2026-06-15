using Amora.Domain.Entities;

namespace Amora.Application.Abstractions;

public interface IAdminNotifier
{
    Task NotifyNewAdminAlertAsync(AdminNotification notification, CancellationToken cancellationToken = default);
}
