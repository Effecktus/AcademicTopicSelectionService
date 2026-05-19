using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Notifications;

public sealed class NotificationsService(
    INotificationsRepository repository,
    IUsersRepository usersRepository,
    IEmailTaskChannel emailTaskChannel) : INotificationsService
{
    public Task<PagedResult<NotificationDto>> GetForCurrentUserAsync(
        ListNotificationsQuery query,
        Guid userId,
        CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };
        return repository.ListByUserAsync(userId, normalized, ct);
    }

    public async Task<Result<bool, NotificationsError>> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(notificationId, ct);
        if (entity is null)
            return Result<bool, NotificationsError>.Fail(NotificationsError.NotFound, "Notification not found");

        if (entity.UserId != userId)
            return Result<bool, NotificationsError>.Fail(NotificationsError.Forbidden, "You can mark only your notifications");

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await repository.SaveChangesAsync(ct);
        }

        return Result<bool, NotificationsError>.Ok(true);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct)
    {
        await repository.MarkAllAsReadAsync(userId, DateTime.UtcNow, ct);
    }

    public async Task MarkByRelatedEntityAsReadAsync(
        Guid userId,
        string typeCodeName,
        string relatedEntityType,
        Guid relatedEntityId,
        CancellationToken ct)
    {
        if (userId == Guid.Empty || relatedEntityId == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(typeCodeName) || string.IsNullOrWhiteSpace(relatedEntityType))
            return;

        await repository.MarkByRelatedEntityAsReadAsync(
            userId,
            typeCodeName.Trim(),
            relatedEntityType.Trim(),
            relatedEntityId,
            ct);
    }

    public async Task<Notification?> CreateAsync(CreateNotificationCommand command, CancellationToken ct)
    {
        if (command.UserId == Guid.Empty)
            return null;

        if (string.IsNullOrWhiteSpace(command.TypeCodeName) ||
            string.IsNullOrWhiteSpace(command.Title) ||
            string.IsNullOrWhiteSpace(command.Content))
            return null;

        var normalizedRelatedEntityType = string.IsNullOrWhiteSpace(command.RelatedEntityType)
            ? null
            : command.RelatedEntityType.Trim();
        var normalizedRelatedEntityId = command.RelatedEntityId == Guid.Empty
            ? null
            : command.RelatedEntityId;

        if ((normalizedRelatedEntityType is null) != (normalizedRelatedEntityId is null))
        {
            throw new InvalidOperationException(
                "RelatedEntityType and RelatedEntityId must be provided together for notifications.");
        }

        var type = await repository.GetTypeByCodeNameAsync(command.TypeCodeName.Trim(), ct);
        if (type is null)
            return null;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TypeId = type.Id,
            Type = type,
            Title = command.Title.Trim(),
            Content = command.Content.Trim(),
            RelatedEntityType = normalizedRelatedEntityType,
            RelatedEntityId = normalizedRelatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        repository.Add(notification);
        return notification;
    }

    /// <inheritdoc />
    public async Task<Notification?> CreateAndSaveAsync(CreateNotificationCommand command, CancellationToken ct)
    {
        var notification = await CreateAsync(command, ct);
        if (notification is not null)
            await repository.SaveChangesAsync(ct);

        return notification;
    }

    public async ValueTask EnqueueEmailAsync(Guid userId, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            return;

        var user = await usersRepository.GetByIdAsync(userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return;

        await emailTaskChannel.WriteAsync(
            new EmailTask(user.Email, subject.Trim(), body.Trim()),
            ct);
    }
}
