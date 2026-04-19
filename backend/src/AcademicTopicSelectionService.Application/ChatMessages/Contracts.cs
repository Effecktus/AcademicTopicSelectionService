namespace AcademicTopicSelectionService.Application.ChatMessages;

/// <summary>
/// Ошибки операций с чатом по заявке.
/// </summary>
public enum ChatMessagesError
{
    NotFound,
    Forbidden,
    Validation
}

/// <summary>
/// Отправка сообщения в чат заявки.
/// </summary>
public sealed record SendMessageCommand(Guid ApplicationId, string Content);

/// <summary>
/// Сообщение чата для API.
/// </summary>
public sealed record ChatMessageDto(
    Guid Id,
    Guid ApplicationId,
    Guid SenderId,
    string SenderFullName,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt);
