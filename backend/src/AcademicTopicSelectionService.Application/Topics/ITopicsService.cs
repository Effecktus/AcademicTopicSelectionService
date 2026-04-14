using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Topics;

/// <summary>
/// Сервис управления темами ВКР.
/// </summary>
public interface ITopicsService
{
    Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct);

    Task<TopicDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создать тему ВКР. Только для аутентифицированных пользователей.
    /// </summary>
    /// <param name="command">Команда на создание.</param>
    /// <param name="createdByUserId">Идентификатор пользователя-создателя (из JWT sub).</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Result<TopicDto, TopicsError>> CreateAsync(CreateTopicCommand command, Guid createdByUserId, CancellationToken ct);

    /// <summary>
    /// Полностью заменить тему ВКР (PUT). Все обязательные поля должны быть указаны.
    /// Только автор темы может заменить.
    /// </summary>
    Task<Result<TopicDto, TopicsError>> ReplaceAsync(Guid id, ReplaceTopicCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Частично обновить тему ВКР (PATCH). Поля со значением <c>null</c> не изменяются.
    /// Только автор темы может редактировать.
    /// </summary>
    Task<Result<TopicDto, TopicsError>> UpdateAsync(Guid id, UpdateTopicCommand command, Guid callerUserId, CancellationToken ct);

    /// <summary>
    /// Удалить тему ВКР. Только автор может удалить.
    /// Нельзя удалить тему, на которую есть заявки.
    /// </summary>
    /// <param name="id">Идентификатор темы.</param>
    /// <param name="callerUserId">Идентификатор вызывающего пользователя (из JWT sub).</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Result<bool, TopicsError>> DeleteAsync(Guid id, Guid callerUserId, CancellationToken ct);
}
