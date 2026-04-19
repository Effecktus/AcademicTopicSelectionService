using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.ChatMessages;

/// <summary>
/// Чат по заявке (студент и научрук из связанного запроса); polling без WebSocket.
/// </summary>
public interface IChatMessagesService
{
    Task<Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError>> GetMessagesAsync(
        Guid applicationId, Guid? afterId, int? limit, Guid userId, CancellationToken ct);

    Task<Result<ChatMessageDto, ChatMessagesError>> SendMessageAsync(
        SendMessageCommand command, Guid senderUserId, CancellationToken ct);

    Task<Result<bool, ChatMessagesError>> MarkAsReadAsync(
        Guid applicationId, Guid readerUserId, CancellationToken ct);
}
