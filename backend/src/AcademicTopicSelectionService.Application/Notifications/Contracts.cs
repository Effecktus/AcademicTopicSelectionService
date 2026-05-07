using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Notifications;

public sealed record CreateNotificationCommand(
    Guid UserId,
    string TypeCodeName,
    string Title,
    string Content,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null);

public sealed record NotificationDto(
    Guid Id,
    string TypeCodeName,
    string TypeDisplayName,
    string Title,
    string Content,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt);

public sealed record ListNotificationsQuery(
    bool? IsRead = null,
    int Page = 1,
    int PageSize = 50);

public enum NotificationsError
{
    Validation,
    NotFound,
    Forbidden
}
