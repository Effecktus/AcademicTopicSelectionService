using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

public interface INotificationsRepository
{
    Task<PagedResult<NotificationDto>> ListByUserAsync(
        Guid userId,
        ListNotificationsQuery query,
        CancellationToken ct);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);

    void Add(Notification notification);

    Task<int> MarkAllAsReadAsync(Guid userId, DateTime readAtUtc, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    Task<NotificationType?> GetTypeByCodeNameAsync(string codeName, CancellationToken ct);
}
