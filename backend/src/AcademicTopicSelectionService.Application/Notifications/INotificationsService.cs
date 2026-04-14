using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Notifications;

public interface INotificationsService
{
    Task<PagedResult<NotificationDto>> GetForCurrentUserAsync(
        ListNotificationsQuery query,
        Guid userId,
        CancellationToken ct);

    Task<Result<bool, NotificationsError>> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct);

    Task<Notification?> CreateAsync(CreateNotificationCommand command, CancellationToken ct);

    ValueTask EnqueueEmailAsync(Guid userId, string subject, string body, CancellationToken ct);
}
