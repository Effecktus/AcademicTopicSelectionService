using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.ChatMessages;

/// <summary>
/// Сообщения чата по заявке: студент-владелец и преподаватель из запроса на научрука; отправка только пока заявка и запрос в активных статусах.
/// </summary>
public sealed class ChatMessagesService(
    IStudentApplicationsRepository applicationsRepo,
    IChatMessagesRepository chatRepo,
    IUsersRepository usersRepo,
    INotificationsService notificationsService) : IChatMessagesService
{
    private const int DefaultLimit = 50;
    private const int MaxContentLength = 4000;
    private const int NotificationContentPreviewMax = 400;

    private static readonly HashSet<string> AllowedSupervisorRequestStatuses = new(StringComparer.Ordinal)
    {
        "Pending",
        "ApprovedBySupervisor"
    };

    /// <summary>Статусы заявки, при которых доступна отправка в чат (совпадает с UI).</summary>
    private static readonly HashSet<string> ChatActiveApplicationStatuses = new(StringComparer.Ordinal)
    {
        "OnEditing",
        "Pending",
        "ApprovedBySupervisor",
        "PendingDepartmentHead"
    };

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError>> GetMessagesAsync(
        Guid applicationId, Guid? afterId, int? limit, Guid userId, CancellationToken ct)
    {
        var access = await applicationsRepo.GetChatAccessAsync(applicationId, ct);
        if (access is null)
            return Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError>.Fail(
                ChatMessagesError.NotFound, "Application not found");

        if (!IsParticipant(userId, access))
            return Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError>.Fail(
                ChatMessagesError.Forbidden, "You are not a participant in this chat");

        var take = limit is null or < 1 ? DefaultLimit : Math.Min(limit.Value, 200);
        var rows = await chatRepo.GetByApplicationAsync(applicationId, afterId, take, ct);
        var dtos = rows.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError>.Ok(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessageDto, ChatMessagesError>> SendMessageAsync(
        SendMessageCommand command, Guid senderUserId, CancellationToken ct)
    {
        var access = await applicationsRepo.GetChatAccessAsync(command.ApplicationId, ct);
        if (access is null)
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.NotFound, "Application not found");

        if (!IsParticipant(senderUserId, access))
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.Forbidden, "You are not a participant in this chat");

        if (!CanUseChat(access))
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.Forbidden, "Chat is not available for this application");

        var content = command.Content.Trim();
        if (content.Length == 0)
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.Validation, "Content is required");

        if (content.Length > MaxContentLength)
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.Validation, $"Content must be at most {MaxContentLength} characters");

        var entity = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = command.ApplicationId,
            SenderId = senderUserId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        var saved = await chatRepo.AddAsync(entity, ct);
        var sender = await usersRepo.GetByIdAsync(senderUserId, ct);
        if (sender is null)
            return Result<ChatMessageDto, ChatMessagesError>.Fail(
                ChatMessagesError.NotFound, "Sender not found");

        var recipientUserId = senderUserId == access.StudentUserId
            ? access.TeacherUserId
            : access.StudentUserId;

        var senderName = FormatUserDisplayName(sender);
        var preview = TruncateForPreview(content, NotificationContentPreviewMax);
        var notification = await notificationsService.CreateAsync(
            new CreateNotificationCommand(
                recipientUserId,
                NotificationTypeCodes.NewMessage,
                "Новое сообщение в чате",
                $"{senderName} написал(а) по заявке: «{preview}»."),
            ct);

        // Ответ в чате подразумевает, что исходящие собеседника уже просмотрены (иначе readAt не обновлялся бы при одном только polling).
        await chatRepo.MarkIncomingAsReadAsync(command.ApplicationId, senderUserId, ct);

        await chatRepo.SaveChangesAsync(ct);

        if (notification is not null)
        {
            await notificationsService.EnqueueEmailAsync(
                notification.UserId,
                notification.Title,
                notification.Content,
                ct);
        }

        saved.Sender = sender;
        return Result<ChatMessageDto, ChatMessagesError>.Ok(MapToDto(saved));
    }

    /// <inheritdoc />
    public async Task<Result<bool, ChatMessagesError>> MarkAsReadAsync(
        Guid applicationId, Guid readerUserId, CancellationToken ct)
    {
        var access = await applicationsRepo.GetChatAccessAsync(applicationId, ct);
        if (access is null)
            return Result<bool, ChatMessagesError>.Fail(ChatMessagesError.NotFound, "Application not found");

        if (!IsParticipant(readerUserId, access))
            return Result<bool, ChatMessagesError>.Fail(
                ChatMessagesError.Forbidden, "You are not a participant in this chat");

        if (!CanUseChat(access))
            return Result<bool, ChatMessagesError>.Fail(
                ChatMessagesError.Forbidden, "Chat is not available for this application");

        await chatRepo.MarkIncomingAsReadAsync(applicationId, readerUserId, ct);
        return Result<bool, ChatMessagesError>.Ok(true);
    }

    private static bool IsParticipant(Guid userId, ApplicationChatAccessInfo access) =>
        userId == access.StudentUserId || userId == access.TeacherUserId;

    private static bool CanUseChat(ApplicationChatAccessInfo access)
    {
        if (!access.HasSupervisorRequest || access.SupervisorRequestStatusCode is null)
            return false;

        if (!AllowedSupervisorRequestStatuses.Contains(access.SupervisorRequestStatusCode))
            return false;

        return ChatActiveApplicationStatuses.Contains(access.ApplicationStatusCode);
    }

    private static ChatMessageDto MapToDto(ChatMessage m)
    {
        var fullName = FormatUserDisplayName(m.Sender!);
        return new ChatMessageDto(
            m.Id,
            m.ApplicationId,
            m.SenderId,
            fullName,
            m.Content,
            m.SentAt,
            m.ReadAt);
    }

    private static string FormatUserDisplayName(User u)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(u.FirstName)) parts.Add(u.FirstName.Trim());
        if (!string.IsNullOrWhiteSpace(u.MiddleName)) parts.Add(u.MiddleName.Trim());
        if (!string.IsNullOrWhiteSpace(u.LastName)) parts.Add(u.LastName.Trim());
        return string.Join(' ', parts);
    }

    private static string TruncateForPreview(string text, int maxLen)
    {
        if (text.Length <= maxLen)
            return text;

        return text[..maxLen] + "…";
    }
}
