namespace AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;

/// <summary>
/// Сервис бизнес-логики для работы со статусами тем ВКР.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface ITopicStatusesService
{
    /// <summary>
    /// Получает постраничный список статусов темы с нормализацией параметров запроса.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком статусов темы.</returns>
    Task<PagedResult<TopicStatusDto>> ListAsync(ListTopicStatusesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает статус темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус темы или <c>null</c>, если не найден.</returns>
    Task<TopicStatusDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новый статус темы с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="command">Данные для создания статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданный статус темы или ошибка.</returns>
    Task<Result<TopicStatusDto, TopicStatusesError>> CreateAsync(UpsertTopicStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет статус темы (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="command">Новые данные статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус темы или ошибка.</returns>
    Task<Result<TopicStatusDto, TopicStatusesError>> UpdateAsync(Guid id, UpsertTopicStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет статус темы (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус темы или ошибка.</returns>
    Task<Result<TopicStatusDto, TopicStatusesError>> PatchAsync(Guid id, UpsertTopicStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет статус темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус был удалён; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
