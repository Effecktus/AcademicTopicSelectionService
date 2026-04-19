using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий сообщений чата по заявке.
/// </summary>
public interface IChatMessagesRepository
{
    /// <summary>
    /// Сообщения заявки: при <paramref name="afterId"/> — только более новые, чем курсорное сообщение; иначе последние <paramref name="limit"/> сообщений (хронологически).
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetByApplicationAsync(
        Guid applicationId, Guid? afterId, int limit, CancellationToken ct);

    Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken ct);

    Task MarkIncomingAsReadAsync(Guid applicationId, Guid readerUserId, CancellationToken ct);
}
